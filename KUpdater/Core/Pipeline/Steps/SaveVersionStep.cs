// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Core.Event;
using KUpdater.Scripting.Runtime;

namespace KUpdater.Core.Pipeline.Steps;

[PipelineStep(40)]
public class SaveVersionStep(string rootDirectory) : IUpdateStep {
    private readonly string _localVersionFile = Path.Combine(rootDirectory, "version.txt");

    public string Name => "SaveVersion";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        File.WriteAllText(_localVersionFile, ctx.Metadata.Version);

        eventManager.NotifyAll(new StatusEvent(
            Localization.Translate("status.update_applied")
        ));

        await Task.CompletedTask;
    }
}
