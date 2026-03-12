// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.Events;

public enum WindowState { Restored, Minimized, Maximized }
public enum FocusState { Focused, LostFocus }
public enum MouseButton { None, Left, Right, Middle }
public enum KeyCode {
    None, A, B, C, D, E, F, G, H, I, J, K, L, M, N,
    O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    Enter, Escape, Space, Backspace, Tab,
    Shift, Control, Alt, Left, Right, Up, Down
}

public record ResizeEvent(int Width, int Height) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record ShownEvent() : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record ClosedEvent(bool UserInitiated) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record StateEvent(WindowState State) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record FocusEvent(FocusState State) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record MouseEvent(int X, int Y, MouseButton Button, int Delta, int Clicks) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record KeyEvent(KeyCode Key, bool IsRepeat) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record TextInputEvent(char Character) : IEvent {
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
