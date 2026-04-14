// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Logging;
using Kx.Core.Logging;
using Kx.Utility;

namespace Kx.App;

/// <summary>
/// Builds the shared logging services for one runtime instance.
/// </summary>
public sealed class RuntimeLoggingComposition {
    private const string SystemCategory = "System";

    /// <summary>
    /// Initializes a new runtime logging composition.
    /// </summary>
    public RuntimeLoggingComposition() {
        DebugLogSink = new AsyncLogSink(new DebugSink());
        FileLogSink = new AsyncLogSink(
            new DailyRollingFileSink(
                5 * 1024 * 1024,
                5,
                () => Paths.GetDailyLogFile()));
    }

    /// <summary>
    /// Gets the debug log sink shared by the runtime.
    /// </summary>
    public AsyncLogSink DebugLogSink { get; }

    /// <summary>
    /// Gets the file log sink shared by the runtime.
    /// </summary>
    public AsyncLogSink FileLogSink { get; }

    /// <summary>
    /// Creates the logger factory for the runtime container.
    /// </summary>
    /// <param name="services">The built dependency container used for logger resolution.</param>
    public LoggerFactory CreateLoggerFactory(IDependencyContainer services) {
        ArgumentNullException.ThrowIfNull(services);
        return new LoggerFactory(services);
    }

    /// <summary>
    /// Creates the system logging service for the runtime container.
    /// </summary>
    /// <param name="services">The built dependency container used for logger resolution.</param>
    public ILoggingService CreateSystemLogger(IDependencyContainer services) {
        ArgumentNullException.ThrowIfNull(services);
        return services.Get<ILoggerFactory>().CreateLogger(SystemCategory);
    }
}
