// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Events;

namespace KUpdater.Core.Event;

public record StatusEvent(string Text) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record ProgressEvent(int Percent) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record ChangelogEvent(string Text) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record UpdateStepStarted(string StepName) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record UpdateStepCompleted(string StepName) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record UpdatePipelineStarted() : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record UpdatePipelineCompleted() : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record UpdateRequired() : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record MainWindow_OnShown() : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record MainWindow_OnFormClosed(bool IsUserInitiated) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record MainWindow_OnStateChanged(WindowState State) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

public record MainWindow_OnFocusChanged(FocusState State) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
};

