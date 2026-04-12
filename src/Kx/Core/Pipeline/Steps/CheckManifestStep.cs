// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Extensions;
using Kx.Core.Localization;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(20)]
public class CheckManifestStep : IUpdateStep {
    public string Name => "CheckManifest";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        bool needsUpdate = HasManifestDifferences(ctx);

        if (!needsUpdate) {
            eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.UpToDateGeneric)));
            throw new OperationCanceledException("No update required");
        }

        eventManager.NotifyAll(new StatusEvent(LanguageService.Translate(KxLanguageKeys.Status.UpdateRequiredGeneric)));
        eventManager.NotifyAll(new UpdateRequired());

        await Task.CompletedTask;
    }

    private static bool HasManifestDifferences(UpdateContext ctx) {
        ArgumentNullException.ThrowIfNull(ctx);

        foreach (var file in ctx.Metadata.Files ?? []) {
            var localFile = new FileInfo(Path.Combine(ctx.RootDirectory, file.Path));
            if (!localFile.VerifySha256(file.Sha256))
                return true;
        }

        foreach (var deletedFile in ctx.Metadata.DeletedFiles ?? []) {
            if (File.Exists(Path.Combine(ctx.RootDirectory, deletedFile)))
                return true;
        }

        return false;
    }
}
