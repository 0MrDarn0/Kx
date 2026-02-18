// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace KUpdater.UI.Interface;

public interface IRenderer : IDisposable {
    void ToggleDebugOverlay();
    void RequestRender();
    void Resize(int width, int height);
    void TogglePerfOverlay();
    void ToggleContentRectDebug();
    SKRect GetContentRect(Size size);

    long LastRenderDurationMs { get; }
    int LastPresentError { get; }
}
