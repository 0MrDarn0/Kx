// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Rendering;

public interface IWindowRenderer : IDisposable {
    void ToggleDebugOverlay();
    void RequestRender();
    void TogglePerfOverlay();
    void ToggleContentRectDebug();
    long LastRenderDurationMs { get; }
    int LastPresentError { get; }
}
