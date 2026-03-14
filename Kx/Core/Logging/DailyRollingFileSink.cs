// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Logging;

namespace Kx.Core.Logging;

public sealed class DailyRollingFileSink(long maxFileSize, int maxFiles, Func<string> getDailyPath) : ILogSink {
    private readonly object _lock = new();

    public void Write(string category, LogLevel level, string message, Exception? ex) {
        lock (_lock) {
            var basePath = getDailyPath(); // z.B. log_2026-03-08.txt
            RotateIfNeeded(basePath);

            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            File.AppendAllText(basePath, $"[{ts}] [{category}] [{level}] {message}\n");

            if (ex != null)
                File.AppendAllText(basePath, ex + "\n");
        }
    }

    private void RotateIfNeeded(string basePath) {
        var file = new FileInfo(basePath);
        if (!file.Exists || file.Length < maxFileSize)
            return;

        var oldest = $"{basePath}.{maxFiles}";
        if (File.Exists(oldest))
            File.Delete(oldest);

        for (int i = maxFiles - 1; i >= 1; i--) {
            var src = $"{basePath}.{i}";
            var dst = $"{basePath}.{i + 1}";
            if (File.Exists(src))
                File.Move(src, dst, true);
        }

        File.Move(basePath, $"{basePath}.1", true);
    }
}
