// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Events;

namespace KUpdater.Abstractions.Backend;

public interface IWindowBackend : IRenderTarget, IUiThreadInvoker {
    event Action<ShownEvent>? Shown;
    event Action<ClosedEvent>? Closed;

    event Action<ResizeEvent>? Resized;
    event Action<MouseEvent>? MouseMove;
    event Action<MouseEvent>? MouseDown;
    event Action<MouseEvent>? MouseUp;
    event Action<MouseEvent>? MouseWheel;

    event Action<KeyEvent>? KeyDown;
    event Action<KeyEvent>? KeyUp;
    event Action<TextInputEvent>? TextInput;

    event Action<StateEvent>? StateChanged;
    event Action<FocusEvent>? FocusChanged;

    void SetSize(int width, int height);
    void SetPosition(int x, int y);
    void ShowWindow();
    void CloseWindow();

    object? Cursor { get; set; }
}
