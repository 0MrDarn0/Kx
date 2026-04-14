// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI;

namespace Kx.UI.Actions;

internal sealed class MarkupActionContext(IVisualContext visualContext, UIElement source, string actionName, string? argument) : IMarkupActionContext {
    public IVisualContext VisualContext { get; } = visualContext;
    public UIElement Source { get; } = source;
    public string ActionName { get; } = actionName;
    public string? Argument { get; } = argument;
}
