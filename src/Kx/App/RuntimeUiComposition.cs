// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.UI.Actions;
using Kx.UI.Commands;
using Kx.UI.Markup;
using Kx.UI.State;
using Kx.UI.Themes;

namespace Kx.App;

/// <summary>
/// Builds the shared UI and markup services for one runtime instance.
/// </summary>
public sealed class RuntimeUiComposition {
    /// <summary>
    /// Initializes a new runtime UI composition.
    /// </summary>
    public RuntimeUiComposition() {
        ActionRegistry = new MarkupActionRegistry();
        CommandRegistry = new UiCommandRegistry();
        StateStore = new UiStateStore();
        ControlRegistry = new ControlRegistry();
        WindowFrameRegistry = new WindowFrameRegistry();
        WindowContentRegistry = new WindowContentRegistry();

        BuiltInMarkupActionRegistrar.Register(ActionRegistry);
        BuiltInControlRegistrar.Register(ControlRegistry);
    }

    /// <summary>
    /// Gets the markup action registry shared by the runtime.
    /// </summary>
    public MarkupActionRegistry ActionRegistry { get; }

    /// <summary>
    /// Gets the UI command registry shared by the runtime.
    /// </summary>
    public UiCommandRegistry CommandRegistry { get; }

    /// <summary>
    /// Gets the UI state store shared by the runtime.
    /// </summary>
    public UiStateStore StateStore { get; }

    /// <summary>
    /// Gets the control registry shared by the runtime.
    /// </summary>
    public ControlRegistry ControlRegistry { get; }

    /// <summary>
    /// Gets the window frame registry shared by the runtime.
    /// </summary>
    public WindowFrameRegistry WindowFrameRegistry { get; }

    /// <summary>
    /// Gets the window content registry shared by the runtime.
    /// </summary>
    public WindowContentRegistry WindowContentRegistry { get; }
}
