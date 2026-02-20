// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Plugin;

public interface IPluginLogger {
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
}
