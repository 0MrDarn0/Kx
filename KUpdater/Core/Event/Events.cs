// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Event;

public record StatusEvent(string Text) : IEvent;
public record ProgressEvent(int Percent) : IEvent;
public record ChangelogEvent(string Text) : IEvent;
public record UpdateStepStarted(string StepName) : IEvent;
public record UpdateStepCompleted(string StepName) : IEvent;
public record UpdatePipelineStarted() : IEvent;
public record UpdatePipelineCompleted() : IEvent;
public record UpdateRequired() : IEvent;

public record MainWindow_OnShown() : IEvent;
public record MainWindow_OnFormClosed(bool IsUserInitiated) : IEvent;
