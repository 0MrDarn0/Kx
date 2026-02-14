// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Core.Event;
using KUpdater.Core.Extensions;
using KUpdater.Core.Localization;

namespace KUpdater.Core.Pipeline.Steps;

[PipelineStep(20)]
public class CheckVersionStep(string rootDirectory) : IUpdateStep {
    private readonly string _localVersionFile = Path.Combine(rootDirectory, "version.txt");

    public string Name => "CheckVersion";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        // Lokale Version laden
        ctx.CurrentVersion = File.Exists(_localVersionFile)
            ? File.ReadAllText(_localVersionFile).Trim()
            : "0.0.0";

        bool needsUpdate = ctx.CurrentVersion != ctx.Metadata.Version;

        // Falls Version gleich, prüfen wir die Dateien per Hash
        if (!needsUpdate) {
            foreach (var file in ctx.Metadata.Files) {
                var localFile = new FileInfo(Path.Combine(ctx.RootDirectory, file.Path));
                if (!localFile.VerifySha256(file.Sha256)) {
                    needsUpdate = true;
                    break;
                }
            }
        }

        if (!needsUpdate) {
            eventManager.NotifyAll(new StatusEvent(
                LanguageService.Translate("status.up_to_date", ctx.CurrentVersion)
            ));
            // Pipeline hier abbrechen
            throw new OperationCanceledException("No update required");
        }

        eventManager.NotifyAll(new StatusEvent(
            LanguageService.Translate("status.update_required", ctx.CurrentVersion, ctx.Metadata.Version)
        ));

        eventManager.NotifyAll(new UpdateRequired());

        await Task.CompletedTask;
    }
}
