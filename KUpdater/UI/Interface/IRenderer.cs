// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Interface;

public interface IRenderer : IDisposable {
    void ToggleDebugOverlay();
    void RequestRender();
    void Resize(int width, int height);
    void TogglePerfOverlay();

    long LastRenderDurationMs { get; }
    int LastPresentError { get; }
}
