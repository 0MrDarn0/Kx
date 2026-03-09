// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Events;

public sealed record ResizeEvent(int Width, int Height);
public sealed record HotkeyEvent(int Id);
public sealed record ShownEvent();
public sealed record ClosedEvent(bool UserInitiated);
public sealed record StateEvent(WindowState State);
public sealed record FocusEvent(FocusState State);
