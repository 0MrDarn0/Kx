// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Rendering;

public interface IWindowRenderer : IDisposable {
    void RequestRender();
    void ToggleDebugOverlay();
    void TogglePerfOverlay();
    void ToggleContentRectDebug();
}
