// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Event;

public record StatusEvent(string Text);
public record ProgressEvent(int Percent);
public record ChangelogEvent(string Text);
public record UpdateStepStarted(string StepName);
public record UpdateStepCompleted(string StepName);
public record UpdatePipelineStarted();
public record UpdatePipelineCompleted();
public record UpdateRequired();
