// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI.Payloads;

/// <summary>
/// Describes a text update for a target visual expression.
/// </summary>
public sealed record TextUpdatePayload(string TargetId, string Text);
