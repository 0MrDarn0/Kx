// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.Logging;

namespace KUpdater.Core.Logging;

public sealed class Logger : ILogger {
    private readonly string _pluginName;

    public Logger(string pluginName = "") {
        _pluginName = pluginName;
    }

    public void Info(string message)
        => Debug.WriteLine($"[INFO][{_pluginName}] {message}");

    public void Warn(string message)
        => Debug.WriteLine($"[WARN][{_pluginName}] {message}");

    public void Error(string message, Exception? ex = null)
        => Debug.WriteLine($"[ERROR][{_pluginName}] {message}\n{ex}");
}
