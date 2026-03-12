// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.Events;

/// <summary>
/// Basisinterface für alle Event‑DTOs.
/// </summary>
public interface IEvent {
    DateTime OccurredAt { get; }
}
