// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.State;

/// <summary>
/// Represents a typed UI state path.
/// </summary>
public sealed record UiStateKey<T>(string Path) {
    public override string ToString() => Path;
}
