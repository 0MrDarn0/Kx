using System.Text.Json;

using Kx.Core.Event;
using Kx.Core.Pipeline;
using Kx.Core.Pipeline.Steps;
using Kx.Core.Update;
using Kx.Sdk.Events;
using Kx.Sdk.Updater;

namespace Kx.Tests;

public sealed class LoadNewsStepTests {
    [Fact]
    public async Task WhenMetadataLoadsThenItDoesNotRequireNewsFile() {
        var source = new FakeUpdateSource {
            MetadataJson = JsonSerializer.Serialize(new UpdateMetadata {
                Files = []
            })
        };
        var step = new LoadMetadataStep(source, "https://updates.example");
        var context = new UpdateContext(Path.GetTempPath());
        var eventManager = new EventManager();

        await step.ExecuteAsync(context, eventManager);

        Assert.Empty(context.Metadata.Files);
        Assert.Equal(0, source.ChangelogRequestCount);
    }

    [Fact]
    public async Task WhenNewsLoadsThenItPublishesTheNewsDocument() {
        var source = new FakeUpdateSource {
            ChangelogContent = "entries:\n  - title: \"News\"\n    content: |\n      Hello from news.yaml"
        };
        var step = new LoadNewsStep(source, "https://updates.example");
        var context = new UpdateContext(Path.GetTempPath());
        var eventManager = new EventManager();
        ChangelogEvent? publishedEvent = null;

        eventManager.Register<ChangelogEvent>(message => publishedEvent = message);

        await step.ExecuteAsync(context, eventManager);

        Assert.NotNull(publishedEvent);
        Assert.Equal(source.ChangelogContent, publishedEvent!.Text);
        Assert.Equal("https://updates.example/news.yaml", source.LastChangelogUrl);
    }

    private sealed class FakeUpdateSource : IUpdateSource {
        public string MetadataJson { get; set; } = "{}";
        public string ChangelogContent { get; set; } = string.Empty;
        public int ChangelogRequestCount { get; private set; }
        public string? LastChangelogUrl { get; private set; }

        public Task<string> GetMetadataJsonAsync(string metadataUrl, CancellationToken ct = default) {
            return Task.FromResult(MetadataJson);
        }

        public Task<Stream> GetPackageStreamAsync(string packageUrl, CancellationToken ct = default) {
            throw new NotSupportedException();
        }

        public Task<long?> GetPackageSizeAsync(string packageUrl, CancellationToken ct = default) {
            throw new NotSupportedException();
        }

        public Task<string> GetChangelogAsync(string changelogUrl, CancellationToken ct = default) {
            ChangelogRequestCount++;
            LastChangelogUrl = changelogUrl;
            return Task.FromResult(ChangelogContent);
        }
    }
}
