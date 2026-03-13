// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI.Payloads;

/// <summary>
/// Identifies a window definition to open.
/// </summary>
public sealed record OpenWindowPayload(string WindowName);
