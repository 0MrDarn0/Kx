// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Logging;

public interface ILoggerFactory {
    ILoggingService CreateLogger(string category);
}
