// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Core.Lifecycle;
using Kx.UI.Platform;

namespace Kx.App;

/// <summary>
/// Builds the shared lifecycle and tray services for one runtime instance.
/// </summary>
public sealed class RuntimeShellComposition {
    /// <summary>
    /// Initializes a new runtime shell composition.
    /// </summary>
    public RuntimeShellComposition() {
        TrayIcon = new TrayIcon();
    }

    /// <summary>
    /// Gets the tray icon configuration shared by the runtime.
    /// </summary>
    public TrayIcon TrayIcon { get; }

    /// <summary>
    /// Creates the startup manager for the runtime container.
    /// </summary>
    /// <param name="services">The built dependency container used during startup.</param>
    public StartupManager CreateStartupManager(IDependencyContainer services) {
        ArgumentNullException.ThrowIfNull(services);
        return new StartupManager(services);
    }

    /// <summary>
    /// Creates the shutdown manager for the runtime container.
    /// </summary>
    /// <param name="services">The built dependency container used during shutdown.</param>
    public ShutdownManager CreateShutdownManager(IDependencyContainer services) {
        ArgumentNullException.ThrowIfNull(services);
        return new ShutdownManager(services);
    }
}
