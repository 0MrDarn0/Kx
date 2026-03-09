// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Events;

public sealed record KeyEvent(KeyCode Key, bool IsRepeat);
public sealed record TextInputEvent(char Character);
public sealed record MouseEvent(int X, int Y, MouseButton Button, int Delta, int Clicks);
