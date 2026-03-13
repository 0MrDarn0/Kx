// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.Events;

public sealed record MarkupActionEvent(string EventName, string SourceId, string? Argument = null) : IEvent {
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public UiEventPayload Payload { get; } = new(Argument);
}
