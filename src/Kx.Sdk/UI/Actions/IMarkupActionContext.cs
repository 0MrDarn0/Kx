// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Elements;

namespace Kx.Sdk.UI.Actions;

/// <summary>
/// Provides context for executing a markup-defined UI action.
/// </summary>
public interface IMarkupActionContext {
    IVisualContext VisualContext { get; }
    UIElement Source { get; }
    string ActionName { get; }
    string? Argument { get; }
}
