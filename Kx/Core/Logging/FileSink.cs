// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Logging;

namespace Kx.Core.Logging;

public sealed class FileSink(string path) : ILogSink {
    public void Write(string category, LogLevel level, string message, Exception? ex) {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        File.AppendAllText(path, $"[{ts}] [{category}] [{level}] {message}\n");

        if (ex != null)
            File.AppendAllText(path, ex + "\n");
    }
}
