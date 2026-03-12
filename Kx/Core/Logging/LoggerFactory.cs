// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;
using Kx.Abstractions.Logging;

namespace Kx.Core.Logging;

public sealed class LoggerFactory(IDependencyContainer container) : ILoggerFactory {
    public ILoggingService CreateLogger(string category)
        => new Logger(category, container.GetAll<ILogSink>());
}
