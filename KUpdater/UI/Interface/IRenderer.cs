// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Interface;

public interface IRenderer : IDisposable {
    void RequestRender();
    void Render();
    void Resize(int width, int height);
    bool IsRendering { get; }
    long LastRenderDurationMs { get; }
    int LastPresentError { get; }
}
