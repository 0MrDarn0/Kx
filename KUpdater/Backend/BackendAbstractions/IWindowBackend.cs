// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)
using KUpdater.Utility;

namespace KUpdater.Backend.BackendAbstractions;

public interface IWindowBackend : IRenderTarget, IUiThreadInvoker {
    event Action<int, int>? BackendResized;
    event Action<MouseEventArgs>? BackendMouseMove;
    event Action<MouseEventArgs>? BackendMouseDown;
    event Action<MouseEventArgs>? BackendMouseUp;
    event Action<MouseEventArgs>? BackendMouseWheel;

    void SetSize(int width, int height);
    void SetPosition(int x, int y);
    void ShowWindow();
    void CloseWindow();

    Cursor? Cursor { get; set; }
    IHotkeyMessageSink? HotkeySink { get; set; }
}
