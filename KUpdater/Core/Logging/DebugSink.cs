// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.Logging;

namespace KUpdater.Core.Logging;

public sealed class DebugSink : ILogSink {

    public void Write(string category, LogLevel level, string message, Exception? ex) {
        var ts = DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.WriteLine($"[{ts}] [{category}] [{level}] {message}");

        if (ex != null)
            Debug.WriteLine(ex);
    }
}
