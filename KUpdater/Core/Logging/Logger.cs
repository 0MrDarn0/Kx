// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Logging;

namespace KUpdater.Core.Logging;

public sealed class Logger : ILoggingService {

    private readonly string _category;

    public Logger(string category) {
        _category = category;
    }

    private static string Format(LogLevel level, string category, string message) {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        return $"[{ts}] [{level}] [{category}] {message}";
    }

    public void Log(LogLevel level, string message, Exception? ex = null) {
        var formatted = Format(level, _category, message);

        System.Diagnostics.Debug.WriteLine(formatted);

        if (ex != null)
            System.Diagnostics.Debug.WriteLine(ex);
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);
    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warning(string message) => Log(LogLevel.Warning, message);
    public void Error(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
    public void Critical(string message, Exception? ex = null) => Log(LogLevel.Critical, message, ex);
}
