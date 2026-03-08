// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Events;

namespace KUpdater.Abstractions.Backend;

public interface IWindowBackend : IRenderTarget, IUiThreadInvoker {
    event Action<WindowResizeEvent>? Resized;
    event Action<WindowMouseEvent>? MouseMove;
    event Action<WindowMouseEvent>? MouseDown;
    event Action<WindowMouseEvent>? MouseUp;
    event Action<WindowMouseEvent>? MouseWheel;
    event Action? Shown;
    event Action<bool /* userInitiated */>? Closed;


    void SetSize(int width, int height);
    void SetPosition(int x, int y);
    void ShowWindow();
    void CloseWindow();

    object? Cursor { get; set; }
}
