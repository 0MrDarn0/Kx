// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Payloads;

/// <summary>
/// Describes a color update for a target visual expression.
/// </summary>
public sealed record ColorUpdatePayload(string TargetId, string Color);
