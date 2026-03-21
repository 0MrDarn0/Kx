// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Lifecycle;
using Kx.Sdk.Logging;

namespace Kx.Core.Plugin;

public sealed class PluginManager : IShutdownAware {
    private readonly IDependencyContainer _services;
    private readonly PluginRuntimePolicy _policy;
    private bool _servicesConfigured;
    private bool _initialized;

    public PluginManager(IDependencyContainer services, PluginRuntimePolicy? policy = null) {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
        _policy = policy ?? new PluginRuntimeComposition().Policy;
    }

    /// <summary>
    /// Loads plugins and lets them register services before the dependency container is built.
    /// </summary>
    public void ConfigureServices() {
        if (_servicesConfigured)
            return;

        _servicesConfigured = true;
        _policy.ConfigureServices(_services);
    }

    /// <summary>
    /// Initializes all configured plugins after the dependency container has been built.
    /// </summary>
    public void InitializeAll() {
        if (_initialized)
            return;

        ConfigureServices();
        _initialized = true;

        var log = _services.Get<ILoggingService>();
        _policy.InitializePlugins(_services, log);
    }

    /// <summary>
    /// Unloads all plugins in reverse dependency order during shutdown.
    /// </summary>
    public ValueTask ShutdownAsync() {
        var log = _services.Get<ILoggingService>();
        _policy.ShutdownPlugins(log);

        return ValueTask.CompletedTask;
    }
}
