// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Localization;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(40)]
public class SaveVersionStep(string rootDirectory) : IUpdateStep {
    private readonly string _localVersionFile = Path.Combine(rootDirectory, "version.txt");

    public string Name => "SaveVersion";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        if (string.IsNullOrWhiteSpace(ctx.Metadata.Version)) {
            if (File.Exists(_localVersionFile))
                File.Delete(_localVersionFile);
        }
        else {
            File.WriteAllText(_localVersionFile, ctx.Metadata.Version);
        }

        eventManager.NotifyAll(new StatusEvent(
            LanguageService.Translate(KxLanguageKeys.Status.UpdateApplied)
        ));

        await Task.CompletedTask;
    }
}
