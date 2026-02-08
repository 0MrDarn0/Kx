// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.IO.Compression;
using KUpdater.Core.Attributes;
using KUpdater.Core.Event;
using KUpdater.Extensions;
using KUpdater.Scripting.Runtime;

namespace KUpdater.Core.Pipeline.Steps;

[PipelineStep(30)]
public class DownloadAndExtractStep : IUpdateStep {
    private readonly IUpdateSource _source;

    public DownloadAndExtractStep(IUpdateSource source) {
        _source = source;
    }

    public string Name => "DownloadAndExtract";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        string tempZip = Path.Combine(Path.GetTempPath(), $"kupdater_{Guid.NewGuid():N}.zip");
        string rootFullPath = Path.GetFullPath(ctx.RootDirectory);

        try {
            // Download
            eventManager.NotifyAll(new StatusEvent(Localization.Translate("status.downloading_pkg")));
            await using (var stream = await _source.GetPackageStreamAsync(ctx.Metadata.PackageUrl, ct).ConfigureAwait(false))
            await using (var fs = new FileStream(tempZip, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                byte[] buffer = new byte[8192];
                long totalRead = 0;
                long totalLength = await _source.GetPackageSizeAsync(ctx.Metadata.PackageUrl, ct).ConfigureAwait(false) ?? -1;
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
            eventManager.NotifyAll(new StatusEvent(Localization.Translate("status.extracting_files")));

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
                        throw new InvalidDataException(Localization.Translate("error.invalid_entry_path", entry.FullName));
                    }

                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir))
                        Directory.CreateDirectory(destDir);

                    var tempDest = destinationPath + $".tmp_{Guid.NewGuid():N}";
                    entry.ExtractToFile(tempDest, overwrite: true);

                    var metaFile = Array.Find(ctx.Metadata.Files, f =>
                        string.Equals(f.Path.Replace("\\", "/"), entry.FullName.Replace("\\", "/"),
                            StringComparison.OrdinalIgnoreCase));

                    if (metaFile != null) {
                        var fileInfo = new FileInfo(tempDest);
                        if (!fileInfo.VerifySha256(metaFile.Sha256)) {
                            try { File.Delete(tempDest); }
                            catch { }
                            throw new InvalidDataException(Localization.Translate("error.hash_mismatch", entry.FullName));
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

            eventManager.NotifyAll(new StatusEvent(Localization.Translate("status.update_complete")));
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
