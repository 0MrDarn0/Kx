// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Core.Lifecycle;
using Kx.Core.Plugin;

namespace Kx.App;

/// <summary>
/// Coordinates the runtime lifecycle once services have been registered.
/// </summary>
public sealed class RuntimeLifecycleCoordinator {
    private readonly IDependencyContainer _services;
    private readonly PluginManager _pluginManager;

    /// <summary>
    /// Initializes a new lifecycle coordinator for the runtime container and plugin manager.
    /// </summary>
    /// <param name="services">The dependency container used by the runtime.</param>
    /// <param name="pluginManager">The plugin manager participating in startup and shutdown.</param>
    public RuntimeLifecycleCoordinator(IDependencyContainer services, PluginManager pluginManager) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(pluginManager);

        _services = services;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// Builds the container, initializes plugins, and runs startup lifecycle services.
    /// </summary>
    public async Task StartAsync() {
        _pluginManager.ConfigureServices();
        _services.Build();
        _pluginManager.InitializeAll();
        await _services.Get<StartupManager>().StartupAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Runs shutdown lifecycle services for the configured runtime.
    /// </summary>
    public async Task ShutdownAsync() {
        await _services.Get<ShutdownManager>().ShutdownAsync().ConfigureAwait(false);
    }
}
