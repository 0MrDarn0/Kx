// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;

namespace Kx.UI.Commands;

internal sealed class UiCommandContext(IVisualContext visualContext, UIElement source, string commandName, string? argument) : IUiCommandContext {
    public IVisualContext VisualContext { get; } = visualContext;
    public UIElement Source { get; } = source;
    public string CommandName { get; } = commandName;
    public string? Argument { get; } = argument;
    public UiCommandPayload Payload { get; } = new(argument);
}
