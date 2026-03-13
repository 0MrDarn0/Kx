// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI.Payloads;

/// <summary>
/// Describes an enabled-state update for a target visual expression.
/// </summary>
public sealed record EnabledStatePayload(string TargetId, bool Enabled);
