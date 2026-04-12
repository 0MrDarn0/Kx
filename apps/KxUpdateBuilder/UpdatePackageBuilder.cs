// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.Encodings.Web;
using System.Text.Json;

using Kx.Core.Extensions;
using Kx.Sdk.Updater;

namespace KxUpdateBuilder;

internal sealed class UpdatePackageBuilder {
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

        string[] updateFiles = Directory.GetFiles(updateFolder, "*", SearchOption.AllDirectories);
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

        List<string> deletedFiles = previousMetadata.Files
            .Select(static file => file.Path)
            .Except(files.Select(static file => file.Path), StringComparer.OrdinalIgnoreCase)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

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

    private static UpdateMetadata ReadPreviousMetadata(string outputJson) {
        if (!File.Exists(outputJson))
            return new UpdateMetadata();

        string json = File.ReadAllText(outputJson);
        return JsonSerializer.Deserialize<UpdateMetadata>(json) ?? new UpdateMetadata();
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

    private static void DeleteIfExists(string filePath) {
        if (File.Exists(filePath))
            File.Delete(filePath);
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
