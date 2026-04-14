// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;

namespace Kx.Sdk.WindowHost;

public interface IWindowHost : IWindowSurface, IUiDispatcher {
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

    /// <summary>
    /// Applies an optional icon to the native host window.
    /// </summary>
    /// <param name="iconStream">A readable stream containing icon data, or <see langword="null"/> to clear the custom icon.</param>
    void SetWindowIcon(Stream? iconStream);

    object? Cursor { get; set; }
}
