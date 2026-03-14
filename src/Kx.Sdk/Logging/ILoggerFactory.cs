// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Logging;

public interface ILoggerFactory {
    ILoggingService CreateLogger(string category);
}
