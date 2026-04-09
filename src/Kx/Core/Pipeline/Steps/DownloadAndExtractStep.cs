// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.IO.Compression;
using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Extensions;
using Kx.Core.Localization;
using Kx.Core.Update;
using Kx.Sdk.Updater;


namespace Kx.Core.Pipeline.Steps;

[PipelineStep(30)]
public class DownloadAndExtractStep(IUpdateSource source) : IUpdateStep {
    public string Name => "DownloadAndExtract";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        string tempZip = Path.Combine(Path.GetTempPath(), $"{UpdaterConstants.TempZipPrefix}{Guid.NewGuid():N}.zip");
        string rootFullPath = Path.GetFullPath(ctx.RootDirectory);

        try {
            // Download
            eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.DownloadingPackage)));
            await using (var stream = await source.GetPackageStreamAsync(ctx.Metadata.PackageUrl, ct).ConfigureAwait(false))
            await using (var fs = new FileStream(tempZip, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                byte[] buffer = new byte[UpdaterConstants.BufferSize];
                long totalRead = 0;
                long totalLength = await source.GetPackageSizeAsync(ctx.Metadata.PackageUrl, ct).ConfigureAwait(false) ?? -1;
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0) {
                    await fs.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                    totalRead += read;
                    if (totalLength > 0) {
                        int percent = (int)((totalRead * 100L) / totalLength);
                        eventManager.NotifyAll(new ProgressEvent(percent));
                    }
                }
            }

            // Extract
            eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.ExtractingFiles)));

            using (var archive = ZipFile.OpenRead(tempZip)) {
                int count = archive.Entries.Count;
                int current = 0;

                foreach (var entry in archive.Entries) {
                    ct.ThrowIfCancellationRequested();

                    // Skip directory entries
                    if (string.IsNullOrEmpty(entry.Name)) {
                        current++;
                        eventManager.NotifyAll(new ProgressEvent(100 * current / count));
                        continue;
                    }

                    var normalizedEntry = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    var destinationPath = Path.GetFullPath(Path.Combine(rootFullPath, normalizedEntry));
                    if (!destinationPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase)) {
                        throw new InvalidDataException(LanguageService.Translate(KxLanguageKeys.Error.InvalidEntryPath, entry.FullName));
                    }

                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir))
                        Directory.CreateDirectory(destDir);

                    var tempDest = destinationPath + $"{UpdaterConstants.TempFileSuffixFormat}{Guid.NewGuid():N}";
                    entry.ExtractToFile(tempDest, overwrite: true);

                    var metaFile = Array.Find([.. ctx.Metadata.Files], f =>
                        string.Equals(f.Path.Replace("\\", "/"), entry.FullName.Replace("\\", "/"),
                            StringComparison.OrdinalIgnoreCase));

                    if (metaFile != null) {
                        var fileInfo = new FileInfo(tempDest);
                        if (!fileInfo.VerifySha256(metaFile.Sha256)) {
                            try { File.Delete(tempDest); }
                            catch { }
                            throw new InvalidDataException(LanguageService.Translate(KxLanguageKeys.Error.HashMismatch, entry.FullName));
                        }
                    }

                    if (File.Exists(destinationPath)) {
                        File.Delete(destinationPath);
                    }
                    File.Move(tempDest, destinationPath);

                    current++;
                    eventManager.NotifyAll(new ProgressEvent(100 * current / count));
                }
            }

            eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.UpdateComplete)));
        }
        finally {
            try {
                if (File.Exists(tempZip))
                    File.Delete(tempZip);
            }
            catch {
                // ignore cleanup failures
            }
        }
    }
}
