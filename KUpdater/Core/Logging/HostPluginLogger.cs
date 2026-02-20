// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Plugin;

namespace KUpdater.Core.Logging;

public sealed class HostPluginLogger : IPluginLogger {
    private readonly string _pluginName;

    public HostPluginLogger(string pluginName) {
        _pluginName = pluginName;
    }

    public void Info(string message)
        => Console.WriteLine($"[INFO][{_pluginName}] {message}");

    public void Warn(string message)
        => Console.WriteLine($"[WARN][{_pluginName}] {message}");

    public void Error(string message, Exception? ex = null)
        => Console.WriteLine($"[ERROR][{_pluginName}] {message}\n{ex}");
}
