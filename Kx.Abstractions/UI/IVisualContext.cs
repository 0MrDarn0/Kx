// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.WindowHost;

namespace Kx.Abstractions.UI;

/// <summary>
/// Provides the minimum UI services required by visuals and controls.
/// </summary>
public interface IVisualContext {
    float DpiScale { get; }
    IUiDispatcher UiThread { get; }
    IUIElementManager UIElementManager { get; }

    /// <summary>
    /// Requests a new render pass for the current window.
    /// </summary>
    void RequestRender();

    /// <summary>
    /// Requests that the current window should close.
    /// </summary>
    void CloseWindow();
}
