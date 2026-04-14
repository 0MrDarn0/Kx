// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Net;
using System.Text;
using System.Text.Json;

using Kx.Core.Event;
using Kx.Core.Extensions;
using Kx.Core.Pipeline;
using Kx.Core.Pipeline.Steps;
using Kx.Core.Update;
using Kx.Sdk.Updater;

namespace Kx.Tests;

public sealed class FileBasedUpdateFlowTests {
    [Fact]
    public async Task WhenManifestHasNoDifferencesThenCheckManifestUsesGenericUpToDateStatus() {
        string rootDirectory = CreateTempDirectory();
        string filePath = Path.Combine(rootDirectory, "data.txt");
        await File.WriteAllTextAsync(filePath, "same");

        var context = new UpdateContext(rootDirectory) {
            Metadata = new UpdateMetadata {
                Files = [
                    new UpdateFile {
                        Path = "data.txt",
                        Sha256 = new FileInfo(filePath).ComputeSha256()
                    }
                ]
            }
        };
        var eventManager = new EventManager();
        string? statusText = null;
        eventManager.Register<StatusEvent>(message => statusText = message.Text);

        var step = new CheckManifestStep();

        await Assert.ThrowsAsync<OperationCanceledException>(() => step.ExecuteAsync(context, eventManager));

        Assert.Equal("Up to date.", statusText);
    }

    [Fact]
    public async Task WhenManifestContainsDeletedFilesThenCheckManifestRequestsAnUpdate() {
        string rootDirectory = CreateTempDirectory();
        string deletedFilePath = Path.Combine(rootDirectory, "obsolete.txt");
        await File.WriteAllTextAsync(deletedFilePath, "remove me");

        var context = new UpdateContext(rootDirectory) {
            Metadata = new UpdateMetadata {
                DeletedFiles = ["obsolete.txt"]
            }
        };
        var eventManager = new EventManager();
        bool updateRequiredRaised = false;
        string? statusText = null;
        eventManager.Register<UpdateRequired>(_ => updateRequiredRaised = true);
        eventManager.Register<StatusEvent>(message => statusText = message.Text);

        var step = new CheckManifestStep();
        await step.ExecuteAsync(context, eventManager);

        Assert.True(updateRequiredRaised);
        Assert.Equal("Update required.", statusText);
    }

    [Fact]
    public async Task WhenFilesAreAppliedThenOnlyChangedFilesAreDownloadedAndDeletedFilesAreRemoved() {
        string rootDirectory = CreateTempDirectory();
        string unchangedFilePath = Path.Combine(rootDirectory, "same.txt");
        string changedFilePath = Path.Combine(rootDirectory, "changed.txt");
        string deletedFilePath = Path.Combine(rootDirectory, "obsolete.txt");

        await File.WriteAllTextAsync(unchangedFilePath, "same");
        await File.WriteAllTextAsync(changedFilePath, "old");
        await File.WriteAllTextAsync(deletedFilePath, "remove me");

        byte[] changedContent = Encoding.UTF8.GetBytes("new content");
        var source = new FakeUpdateSource();
        source.RegisterFile("https://updates.example/changed.txt", changedContent);

        var context = new UpdateContext(rootDirectory) {
            Metadata = new UpdateMetadata {
                Files = [
                    new UpdateFile {
                        Path = "same.txt",
                        Sha256 = new FileInfo(unchangedFilePath).ComputeSha256()
                    },
                    new UpdateFile {
                        Path = "changed.txt",
                        Sha256 = ComputeSha256(changedContent)
                    }
                ],
                DeletedFiles = ["obsolete.txt"]
            }
        };

        var step = new DownloadAndApplyStep(source, "https://updates.example/");
        await step.ExecuteAsync(context, new EventManager());

        Assert.Equal(["https://updates.example/changed.txt"], source.RequestedUrls);
        Assert.Equal("same", await File.ReadAllTextAsync(unchangedFilePath));
        Assert.Equal("new content", await File.ReadAllTextAsync(changedFilePath));
        Assert.False(File.Exists(deletedFilePath));
    }

    [Fact]
    public async Task WhenCheckingForUpdatesThenTheApplyStepIsNotExecuted() {
        string rootDirectory = CreateTempDirectory();
        var source = new FakeUpdateSource {
            MetadataJson = JsonSerializer.Serialize(new UpdateMetadata {
                Files = [
                    new UpdateFile {
                        Path = "changed.txt",
                        Sha256 = ComputeSha256(Encoding.UTF8.GetBytes("new content"))
                    }
                ]
            })
        };

        var runner = new UpdaterPipelineRunner(new EventManager(), source, "https://updates.example/", rootDirectory);

        bool updateRequired = await runner.CheckForUpdatesAsync(rootDirectory);

        Assert.True(updateRequired);
        Assert.Equal(0, source.PackageStreamRequestCount);
    }

    [Fact]
    public async Task WhenUpdateRunCompletesThenAppliedStatusIsPublished() {
        string rootDirectory = CreateTempDirectory();
        byte[] fileContent = Encoding.UTF8.GetBytes("new content");
        var source = new FakeUpdateSource {
            MetadataJson = JsonSerializer.Serialize(new UpdateMetadata {
                Files = [
                    new UpdateFile {
                        Path = "changed.txt",
                        Sha256 = ComputeSha256(fileContent)
                    }
                ]
            })
        };
        source.RegisterFile("https://updates.example/changed.txt", fileContent);

        var eventManager = new EventManager();
        string? lastStatus = null;
        eventManager.Register<StatusEvent>(message => lastStatus = message.Text);

        var runner = new UpdaterPipelineRunner(eventManager, source, "https://updates.example/", rootDirectory);
        await runner.RunAsync(rootDirectory);

        Assert.Equal("Update applied successfully.", lastStatus);
    }

    [Fact]
    public async Task WhenFileNameContainsPlusThenDownloadUrlUsesEscapedPathSegments() {
        string rootDirectory = CreateTempDirectory();
        byte[] fileContent = Encoding.UTF8.GetBytes("bitmap-data");
        var source = new FakeUpdateSource();
        source.RegisterFile("https://updates.example/data/Hypertext/B%28%2B%29.bmp", fileContent);

        var context = new UpdateContext(rootDirectory) {
            Metadata = new UpdateMetadata {
                Files = [
                    new UpdateFile {
                        Path = "data/Hypertext/B(+).bmp",
                        Sha256 = ComputeSha256(fileContent)
                    }
                ]
            }
        };

        var step = new DownloadAndApplyStep(source, "https://updates.example/");
        await step.ExecuteAsync(context, new EventManager());

        Assert.Equal(["https://updates.example/data/Hypertext/B%28%2B%29.bmp"], source.RequestedUrls);
        Assert.True(File.Exists(Path.Combine(rootDirectory, "data", "Hypertext", "B(+).bmp")));
    }

    [Fact]
    public async Task WhenEscapedPlusPathReturnsNotFoundThenLiteralPathIsRetried() {
        string rootDirectory = CreateTempDirectory();
        byte[] fileContent = Encoding.UTF8.GetBytes("bitmap-data");
        var source = new FakeUpdateSource();
        source.RegisterFile("https://updates.example/data/Hypertext/B(+).bmp", fileContent);

        var context = new UpdateContext(rootDirectory) {
            Metadata = new UpdateMetadata {
                Files = [
                    new UpdateFile {
                        Path = "data/Hypertext/B(+).bmp",
                        Sha256 = ComputeSha256(fileContent)
                    }
                ]
            }
        };

        var step = new DownloadAndApplyStep(source, "https://updates.example/");
        await step.ExecuteAsync(context, new EventManager());

        Assert.Equal([
            "https://updates.example/data/Hypertext/B%28%2B%29.bmp",
            "https://updates.example/data/Hypertext/B(+).bmp"
        ], source.RequestedUrls);
        Assert.True(File.Exists(Path.Combine(rootDirectory, "data", "Hypertext", "B(+).bmp")));
    }

    private static string CreateTempDirectory() {
        string path = Path.Combine(Path.GetTempPath(), "kx-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string ComputeSha256(byte[] content) {
        string filePath = Path.Combine(CreateTempDirectory(), "hash.bin");
        File.WriteAllBytes(filePath, content);
        return new FileInfo(filePath).ComputeSha256();
    }

    private sealed class FakeUpdateSource : IUpdateSource {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        public string MetadataJson { get; set; } = JsonSerializer.Serialize(new UpdateMetadata());
        public List<string> RequestedUrls { get; } = [];
        public int PackageStreamRequestCount { get; private set; }

        public void RegisterFile(string url, byte[] content) {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            ArgumentNullException.ThrowIfNull(content);
            _files[url] = content;
        }

        public Task<string> GetMetadataJsonAsync(string metadataUrl, CancellationToken ct = default) {
            return Task.FromResult(MetadataJson);
        }

        public Task<Stream> GetPackageStreamAsync(string packageUrl, CancellationToken ct = default) {
            RequestedUrls.Add(packageUrl);
            PackageStreamRequestCount++;
            if (!_files.TryGetValue(packageUrl, out var content))
                throw new HttpRequestException($"No file registered for {packageUrl}.", null, HttpStatusCode.NotFound);

            return Task.FromResult<Stream>(new MemoryStream(content, writable: false));
        }

        public Task<long?> GetPackageSizeAsync(string packageUrl, CancellationToken ct = default) {
            return Task.FromResult<long?>(null);
        }

        public Task<string> GetChangelogAsync(string changelogUrl, CancellationToken ct = default) {
            return Task.FromResult(string.Empty);
        }
    }
}
