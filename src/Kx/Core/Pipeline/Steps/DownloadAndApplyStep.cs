// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Net;

using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Extensions;
using Kx.Core.Localization;
using Kx.Core.Update;
using Kx.Sdk.Updater;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(30)]
public class DownloadAndApplyStep(IUpdateSource source, string baseUrl) : IUpdateStep {
    private readonly IUpdateSource _source = source;
    private readonly string _baseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

    public string Name => "DownloadAndApply";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        string rootFullPath = Path.GetFullPath(ctx.RootDirectory);
        List<UpdateFile> filesToApply = GetFilesToApply(ctx, rootFullPath);
        List<PendingDeleteFile> filesToDelete = GetFilesToDelete(ctx, rootFullPath);
        int totalOperations = filesToApply.Count + filesToDelete.Count;
        int completedOperations = 0;

        eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.ApplyingFiles)));

        foreach (var file in filesToApply) {
            ct.ThrowIfCancellationRequested();
            eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.DownloadingFile, file.Path)));

            string destinationPath = GetValidatedDestinationPath(rootFullPath, file.Path);
            string finalPath = IsCurrentProcessExecutable(destinationPath)
                ? GetPendingSelfUpdatePath(destinationPath)
                : destinationPath;

            await DownloadFileAsync(file, finalPath, ct).ConfigureAwait(false);

            completedOperations++;
            ReportProgress(eventManager, completedOperations, totalOperations);
        }

        foreach (var fileToDelete in filesToDelete) {
            ct.ThrowIfCancellationRequested();
            eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.RemovingFile, fileToDelete.RelativePath)));

            if (!IsCurrentProcessExecutable(fileToDelete.FullPath))
                File.Delete(fileToDelete.FullPath);

            completedOperations++;
            ReportProgress(eventManager, completedOperations, totalOperations);
        }

        eventManager.NotifyAll(new ProgressEvent(100));
        eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.UpdateComplete)));
    }

    private async Task DownloadFileAsync(UpdateFile file, string destinationPath, CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        string tempPath = destinationPath + $"{UpdaterConstants.TempFileSuffixFormat}{Guid.NewGuid():N}";

        try {
            await using var sourceStream = await OpenPackageStreamAsync(file.Path, ct).ConfigureAwait(false);
            await using (var destinationStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                await sourceStream.CopyToAsync(destinationStream, ct).ConfigureAwait(false);
            }

            var fileInfo = new FileInfo(tempPath);
            if (!fileInfo.VerifySha256(file.Sha256))
                throw new InvalidDataException(LanguageService.Translate(KxLanguageKeys.Error.HashMismatch, file.Path));

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.Move(tempPath, destinationPath);
        }
        finally {
            if (File.Exists(tempPath)) {
                try {
                    File.Delete(tempPath);
                }
                catch {
                }
            }
        }
    }

    private async Task<Stream> OpenPackageStreamAsync(string relativePath, CancellationToken ct) {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string[] fileUrls = CreateFileUrls(relativePath);
        HttpRequestException? lastNotFoundException = null;

        foreach (string fileUrl in fileUrls) {
            try {
                return await _source.GetPackageStreamAsync(fileUrl, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
                lastNotFoundException = ex;
            }
        }

        if (lastNotFoundException is not null)
            throw lastNotFoundException;

        throw new InvalidOperationException($"No download URL candidates were produced for '{relativePath}'.");
    }

    private string[] CreateFileUrls(string relativePath) {
        string normalizedPath = relativePath.Replace('\\', '/');
        string escapedPath = string.Join('/', normalizedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));

        string escapedUrl = new Uri(new Uri(_baseUrl, UriKind.Absolute), escapedPath).ToString();
        string literalUrl = new Uri(new Uri(_baseUrl, UriKind.Absolute), normalizedPath).ToString();

        return string.Equals(escapedUrl, literalUrl, StringComparison.Ordinal)
            ? [escapedUrl]
            : [escapedUrl, literalUrl];
    }

    private static List<UpdateFile> GetFilesToApply(UpdateContext ctx, string rootFullPath) {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFullPath);

        List<UpdateFile> filesToApply = [];
        foreach (var file in ctx.Metadata.Files ?? []) {
            string destinationPath = GetValidatedDestinationPath(rootFullPath, file.Path);
            string currentPath = IsCurrentProcessExecutable(destinationPath)
                ? GetPendingSelfUpdatePath(destinationPath)
                : destinationPath;

            if (new FileInfo(currentPath).VerifySha256(file.Sha256))
                continue;

            filesToApply.Add(file);
        }

        return filesToApply;
    }

    private static List<PendingDeleteFile> GetFilesToDelete(UpdateContext ctx, string rootFullPath) {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFullPath);

        List<PendingDeleteFile> filesToDelete = [];
        foreach (var deletedFile in ctx.Metadata.DeletedFiles ?? []) {
            string fullPath = GetValidatedDestinationPath(rootFullPath, deletedFile);
            if (!File.Exists(fullPath))
                continue;

            filesToDelete.Add(new PendingDeleteFile(deletedFile, fullPath));
        }

        return filesToDelete;
    }

    private static string GetValidatedDestinationPath(string rootFullPath, string relativePath) {
        string normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        string destinationPath = Path.GetFullPath(Path.Combine(rootFullPath, normalizedPath));

        if (!destinationPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(LanguageService.Translate(KxLanguageKeys.Error.InvalidEntryPath, relativePath));

        return destinationPath;
    }

    private static bool IsCurrentProcessExecutable(string filePath) {
        string? processPath = Environment.ProcessPath;
        return !string.IsNullOrWhiteSpace(processPath) &&
               string.Equals(Path.GetFullPath(filePath), Path.GetFullPath(processPath), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPendingSelfUpdatePath(string executablePath) {
        return Path.Combine(
            Path.GetDirectoryName(executablePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(executablePath) + UpdaterConstants.PendingSelfUpdateSuffix + Path.GetExtension(executablePath));
    }

    private static void ReportProgress(IEventManager eventManager, int completedOperations, int totalOperations) {
        if (totalOperations <= 0)
            return;

        eventManager.NotifyAll(new ProgressEvent((int)((completedOperations * 100f) / totalOperations)));
    }

    private sealed record PendingDeleteFile(string RelativePath, string FullPath);
}
