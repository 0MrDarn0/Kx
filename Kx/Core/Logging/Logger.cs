// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.Logging;

namespace Kx.Core.Logging;

public sealed class Logger(string category, IEnumerable<ILogSink> sinks) : ILoggingService {
    public void Log(LogLevel level, string message, Exception? ex = null) {
        foreach (var sink in sinks) {
            sink.Write(category, level, message, ex);
        }
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);
    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warning(string message) => Log(LogLevel.Warning, message);
    public void Error(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
    public void Critical(string message, Exception? ex = null) => Log(LogLevel.Critical, message, ex);
}
