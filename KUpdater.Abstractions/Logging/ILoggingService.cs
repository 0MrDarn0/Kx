// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Logging;

public interface ILoggingService {
    void Log(LogLevel level, string message, Exception? ex = null);

    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? ex = null);
    void Critical(string message, Exception? ex = null);
}
