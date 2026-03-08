// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Tray;

public interface ITrayIcon : IDisposable {
    ITrayIcon Name(string name);
    ITrayIcon Icon(string resourcePath);
    ITrayIcon StatusIcons(Action<dynamic> configure); // keep dynamic for flexible menu builders
    ITrayIcon Menu(Action<dynamic> configure);
}
public interface ITrayIconFactory {
    ITrayIcon Create();
}
