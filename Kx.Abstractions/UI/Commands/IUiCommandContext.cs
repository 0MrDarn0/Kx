// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI.Elements;

namespace Kx.Abstractions.UI.Commands;

/// <summary>
/// Provides context for executing a UI command.
/// </summary>
public interface IUiCommandContext {
    IVisualContext VisualContext { get; }
    UIElement Source { get; }
    string CommandName { get; }
    string? Argument { get; }
    UiCommandPayload Payload { get; }
}
