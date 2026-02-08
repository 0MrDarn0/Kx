// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;

namespace KUpdater.Core.Pipeline;

public interface IUpdateStep {
    string Name { get; }
    Task ExecuteAsync(UpdateContext context, IEventManager eventManager, CancellationToken ct = default);
}

public class UpdateContext(string rootDirectory) {
    public string RootDirectory { get; } = rootDirectory;
    public UpdateMetadata Metadata { get; set; } = new();
    public string CurrentVersion { get; set; } = "0.0.0";
}

public class UpdatePipeline {
    private readonly List<IUpdateStep> _steps = new();

    public UpdatePipeline AddStep(IUpdateStep step) {
        _steps.Add(step);
        return this;
    }

    public async Task RunAsync(UpdateContext context, IEventManager eventManager, CancellationToken ct = default) {
        eventManager.NotifyAll(new UpdatePipelineStarted());

        foreach (var step in _steps) {
            ct.ThrowIfCancellationRequested();
            eventManager.NotifyAll(new UpdateStepStarted(step.Name));
            await step.ExecuteAsync(context, eventManager, ct);
            eventManager.NotifyAll(new UpdateStepCompleted(step.Name));
        }

        eventManager.NotifyAll(new UpdatePipelineCompleted());
    }
}
