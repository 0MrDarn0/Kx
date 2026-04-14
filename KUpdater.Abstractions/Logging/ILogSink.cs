// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Logging;

public interface ILogSink {
    void Write(string category, LogLevel level, string message, Exception? ex);
}
