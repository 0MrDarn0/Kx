// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Extensions;
using Kx.Core.Localization;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(20)]
public class CheckVersionStep(string rootDirectory) : IUpdateStep {
    private readonly string _localVersionFile = Path.Combine(rootDirectory, "version.txt");

    public string Name => "CheckVersion";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        ctx.CurrentVersion = File.Exists(_localVersionFile)
            ? File.ReadAllText(_localVersionFile).Trim()
            : string.Empty;

        bool needsUpdate = HasManifestDifferences(ctx);

        if (!needsUpdate) {
            eventManager.NotifyAll(new StatusEvent(CreateUpToDateStatusText(ctx)));
            throw new OperationCanceledException("No update required");
        }

        eventManager.NotifyAll(new StatusEvent(CreateUpdateRequiredStatusText(ctx)));
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

    private static string CreateUpToDateStatusText(UpdateContext ctx) {
        return string.IsNullOrWhiteSpace(ctx.Metadata.Version)
            ? LanguageService.Translate(KxLanguageKeys.Status.UpToDateGeneric)
            : LanguageService.Translate(KxLanguageKeys.Status.UpToDate, ctx.Metadata.Version);
    }

    private static string CreateUpdateRequiredStatusText(UpdateContext ctx) {
        return string.IsNullOrWhiteSpace(ctx.CurrentVersion) || string.IsNullOrWhiteSpace(ctx.Metadata.Version)
            ? LanguageService.Translate(KxLanguageKeys.Status.UpdateRequiredGeneric)
            : LanguageService.Translate(KxLanguageKeys.Status.UpdateRequired, ctx.CurrentVersion, ctx.Metadata.Version);
    }
}
