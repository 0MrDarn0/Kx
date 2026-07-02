// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.Encodings.Web;
using System.Text.Json;

using Kx.Core.Extensions;
using Kx.Sdk.Updater;

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KxUpdateBuilder;

internal sealed class UpdatePackageBuilder {
    private static readonly HashSet<string> _excludedFileNames = new(StringComparer.OrdinalIgnoreCase) {
        "Thumbs.db",
        "desktop.ini"
    };

    private static readonly IDeserializer _newsDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer _newsSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public UpdatePackageBuildDefaults CreateDefaults(string workingDirectory) {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        string updateFolder = Path.Combine(workingDirectory, "Update");
        string uploadFolder = Path.Combine(workingDirectory, "Upload");

        return new UpdatePackageBuildDefaults(updateFolder, uploadFolder);
    }

    public UpdatePackageBuildResult Build(UpdatePackageBuildRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        string updateFolder = NormalizeRequiredPath(request.UpdateFolder, nameof(request.UpdateFolder));
        string uploadFolder = NormalizeRequiredPath(request.UploadFolder, nameof(request.UploadFolder));

        Directory.CreateDirectory(updateFolder);
        Directory.CreateDirectory(uploadFolder);

        string[] updateFiles = [.. Directory.GetFiles(updateFolder, "*", SearchOption.AllDirectories).Where(static filePath => !IsExcludedUpdateFile(filePath))];
        if (updateFiles.Length == 0)
            throw new InvalidOperationException("The update folder is empty. Add files before building the update manifest.");

        string outputJson = Path.Combine(uploadFolder, "update.json");
        UpdateMetadata previousMetadata = ReadPreviousMetadata(outputJson);

        if (File.Exists(outputJson) && !request.OverwriteExisting)
            throw new InvalidOperationException("Output files already exist. Enable overwrite and run the build again.");

        DeleteLegacyPackageArtifacts(uploadFolder);

        List<UpdateFile> files = [];
        foreach (string filePath in updateFiles) {
            string relativePath = Path.GetRelativePath(updateFolder, filePath).Replace("\\", "/", StringComparison.Ordinal);
            string uploadPath = Path.Combine(uploadFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));
            string? uploadDirectory = Path.GetDirectoryName(uploadPath);
            if (!string.IsNullOrWhiteSpace(uploadDirectory))
                Directory.CreateDirectory(uploadDirectory);

            File.Copy(filePath, uploadPath, overwrite: true);
            files.Add(new UpdateFile {
                Path = relativePath,
                Sha256 = new FileInfo(filePath).ComputeSha256()
            });
        }

        List<string> deletedFiles = [.. previousMetadata.Files
            .Select(static file => file.Path)
            .Except(files.Select(static file => file.Path), StringComparer.OrdinalIgnoreCase)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)];

        DeleteRemovedFiles(uploadFolder, deletedFiles);

        UpdateMetadata metadata = new() {
            Files = files.OrderBy(static file => file.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            DeletedFiles = deletedFiles
        };

        JsonSerializerOptions options = new() {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(outputJson, JsonSerializer.Serialize(metadata, options));

        return new UpdatePackageBuildResult(
            outputJson,
            metadata.Files.Count,
            metadata.DeletedFiles.Count,
            request.OverwriteExisting);
    }

    public UpdateNewsEditResult AddNewsEntry(UpdateNewsAddRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        string uploadFolder = NormalizeRequiredPath(request.UploadFolder, nameof(request.UploadFolder));
        string title = NormalizeRequiredText(request.Title, nameof(request.Title));
        string content = NormalizeRequiredText(request.Content, nameof(request.Content));

        Directory.CreateDirectory(uploadFolder);

        string newsFilePath = Path.Combine(uploadFolder, "news.yaml");
        NewsDocument document = ReadNewsDocument(newsFilePath);
        document.Entries.Insert(0, new NewsDocumentEntry {
            Title = title,
            Content = content
        });

        WriteNewsDocument(newsFilePath, document);

        return new UpdateNewsEditResult(newsFilePath, document.Entries.Count, 1);
    }

    public UpdateNewsEditResult RemoveNewsEntries(UpdateNewsRemoveRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        string uploadFolder = NormalizeRequiredPath(request.UploadFolder, nameof(request.UploadFolder));
        string title = NormalizeRequiredText(request.Title, nameof(request.Title));

        Directory.CreateDirectory(uploadFolder);

        string newsFilePath = Path.Combine(uploadFolder, "news.yaml");
        NewsDocument document = ReadNewsDocument(newsFilePath);

        int removedCount = document.Entries.RemoveAll(entry =>
            string.Equals(entry.Title?.Trim(), title, StringComparison.OrdinalIgnoreCase));

        if (removedCount > 0)
            WriteNewsDocument(newsFilePath, document);

        return new UpdateNewsEditResult(newsFilePath, document.Entries.Count, removedCount);
    }

    public UpdateNewsListResult LoadNewsEntries(UpdateNewsLoadRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        string uploadFolder = NormalizeRequiredPath(request.UploadFolder, nameof(request.UploadFolder));
        Directory.CreateDirectory(uploadFolder);

        string newsFilePath = Path.Combine(uploadFolder, "news.yaml");
        NewsDocument document = ReadNewsDocument(newsFilePath);

        List<UpdateNewsEntry> entries = [.. document.Entries
            .Select(entry => new UpdateNewsEntry(
                (entry.Title ?? string.Empty).Trim(),
                (entry.Content ?? string.Empty).Trim()))];

        return new UpdateNewsListResult(newsFilePath, entries);
    }

    public UpdateNewsEditResult UpdateNewsEntry(UpdateNewsUpdateRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        string uploadFolder = NormalizeRequiredPath(request.UploadFolder, nameof(request.UploadFolder));
        string title = NormalizeRequiredText(request.Title, nameof(request.Title));
        string content = NormalizeRequiredText(request.Content, nameof(request.Content));

        Directory.CreateDirectory(uploadFolder);

        string newsFilePath = Path.Combine(uploadFolder, "news.yaml");
        NewsDocument document = ReadNewsDocument(newsFilePath);

        if (request.Index < 0 || request.Index >= document.Entries.Count)
            throw new InvalidOperationException("The selected news entry no longer exists.");

        document.Entries[request.Index] = new NewsDocumentEntry {
            Title = title,
            Content = content
        };

        WriteNewsDocument(newsFilePath, document);

        return new UpdateNewsEditResult(newsFilePath, document.Entries.Count, 1);
    }

    public UpdateNewsEditResult RemoveNewsEntry(UpdateNewsRemoveAtRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        string uploadFolder = NormalizeRequiredPath(request.UploadFolder, nameof(request.UploadFolder));

        Directory.CreateDirectory(uploadFolder);

        string newsFilePath = Path.Combine(uploadFolder, "news.yaml");
        NewsDocument document = ReadNewsDocument(newsFilePath);

        if (request.Index < 0 || request.Index >= document.Entries.Count)
            throw new InvalidOperationException("The selected news entry no longer exists.");

        document.Entries.RemoveAt(request.Index);
        WriteNewsDocument(newsFilePath, document);

        return new UpdateNewsEditResult(newsFilePath, document.Entries.Count, 1);
    }

    private static UpdateMetadata ReadPreviousMetadata(string outputJson) {
        if (!File.Exists(outputJson))
            return new UpdateMetadata();

        string json = File.ReadAllText(outputJson);
        return JsonSerializer.Deserialize<UpdateMetadata>(json) ?? new UpdateMetadata();
    }

    private static NewsDocument ReadNewsDocument(string newsFilePath) {
        if (!File.Exists(newsFilePath))
            return new NewsDocument();

        string yaml = File.ReadAllText(newsFilePath);
        if (string.IsNullOrWhiteSpace(yaml))
            return new NewsDocument();

        try {
            var document = _newsDeserializer.Deserialize<NewsDocument>(yaml) ?? new NewsDocument();
            document.Entries ??= [];
            return document;
        }
        catch (YamlException ex) {
            throw new InvalidOperationException($"Could not parse '{newsFilePath}'.", ex);
        }
    }

    private static void WriteNewsDocument(string newsFilePath, NewsDocument document) {
        ArgumentNullException.ThrowIfNull(document);
        document.Entries ??= [];

        using var writer = new StringWriter();
        writer.WriteLine("entries:");

        foreach (NewsDocumentEntry entry in document.Entries) {
            string title = EscapeSingleQuotedYaml((entry.Title ?? string.Empty).Trim());
            string content = NormalizeYamlLiteralContent(entry.Content ?? string.Empty);

            writer.Write("  - title: '");
            writer.Write(title);
            writer.WriteLine("'");
            writer.WriteLine("    content: |");

            foreach (string line in content.Split('\n')) {
                writer.Write("      ");
                writer.WriteLine(line);
            }

            writer.WriteLine();
        }

        File.WriteAllText(newsFilePath, writer.ToString());
    }

    private static string EscapeSingleQuotedYaml(string value) {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static string NormalizeYamlLiteralContent(string value) {
        string normalized = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .TrimEnd('\n');

        return normalized.Length == 0 ? " " : normalized;
    }

    private static bool IsExcludedUpdateFile(string filePath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return _excludedFileNames.Contains(Path.GetFileName(filePath));
    }

    private static void DeleteLegacyPackageArtifacts(string uploadFolder) {
        DeleteIfExists(Path.Combine(uploadFolder, "update.zip"));
        DeleteIfExists(Path.Combine(uploadFolder, "version.txt"));
    }

    private static void DeleteRemovedFiles(string uploadFolder, IEnumerable<string> deletedFiles) {
        foreach (string deletedFile in deletedFiles) {
            string filePath = Path.Combine(uploadFolder, deletedFile.Replace('/', Path.DirectorySeparatorChar));
            DeleteIfExists(filePath);
        }
    }

    private static string NormalizeRequiredPath(string value, string paramName) {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{paramName} must not be empty.");

        return Path.GetFullPath(value.Trim());
    }

    private static string NormalizeRequiredText(string value, string paramName) {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{paramName} must not be empty.");

        return value.Trim();
    }

    private static void DeleteIfExists(string filePath) {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private sealed class NewsDocument {
        public List<NewsDocumentEntry> Entries { get; set; } = [];
    }

    private sealed class NewsDocumentEntry {
        public string? Title { get; set; }
        public string? Content { get; set; }
    }
}

internal sealed record UpdatePackageBuildRequest(
    string UpdateFolder,
    string UploadFolder,
    bool OverwriteExisting);

internal sealed record UpdatePackageBuildDefaults(
    string UpdateFolder,
    string UploadFolder);

internal sealed record UpdatePackageBuildResult(
    string OutputJson,
    int FileCount,
    int DeletedFileCount,
    bool OverwroteExistingFiles);

internal sealed record UpdateNewsAddRequest(
    string UploadFolder,
    string Title,
    string Content);

internal sealed record UpdateNewsRemoveRequest(
    string UploadFolder,
    string Title);

internal sealed record UpdateNewsEditResult(
    string NewsFilePath,
    int EntryCount,
    int ChangedCount);

internal sealed record UpdateNewsLoadRequest(
    string UploadFolder);

internal sealed record UpdateNewsUpdateRequest(
    string UploadFolder,
    int Index,
    string Title,
    string Content);

internal sealed record UpdateNewsRemoveAtRequest(
    string UploadFolder,
    int Index);

internal sealed record UpdateNewsListResult(
    string NewsFilePath,
    IReadOnlyList<UpdateNewsEntry> Entries);

internal sealed record UpdateNewsEntry(
    string Title,
    string Content);
