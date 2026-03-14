// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Actions;

/// <summary>
/// Describes how a markup action target should be resolved.
/// </summary>
public enum UiTargetKind {
    Self,
    Parent,
    Root,
    Id
}
