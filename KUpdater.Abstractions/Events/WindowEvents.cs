// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Events;

public enum MouseButton { None, Left, Right, Middle }
public record WindowMouseEvent(int X, int Y, MouseButton Button, int Delta, int Clicks);
public record WindowResizeEvent(int Width, int Height);
public record HotkeyEvent(int Id);
