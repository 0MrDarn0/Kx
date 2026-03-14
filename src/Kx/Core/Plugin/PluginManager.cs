// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.Sdk.DI;
using Kx.Sdk.Lifecycle;
using Kx.Sdk.Logging;
using Kx.Sdk.Plugin;

namespace Kx.Core.Plugin;

public sealed class PluginManager(IDependencyContainer services) : IShutdownAware {
    private bool _servicesConfigured;
    private bool _initialized;

    /// <summary>
    /// Loads plugins and lets them register services before the dependency container is built.
    /// </summary>
    public void ConfigureServices() {
        if (_servicesConfigured)
            return;

        _servicesConfigured = true;

        var plugins = PluginLoader.LoadAll<IPlugin>();
        foreach (var plugin in plugins) {
            if (plugin is not IServicePlugin servicePlugin)
                continue;

            try {
                Debug.WriteLine($"[PluginManager] Configuring services for plugin: {plugin.Name}");
                servicePlugin.ConfigureServices(services);
            }
            catch (Exception ex) {
                Debug.WriteLine($"[PluginManager] Service registration failed for plugin '{plugin.Name}': {ex}");
                PluginRegistry.Unload(plugin);
            }
        }
    }

    /// <summary>
    /// Initializes all configured plugins after the dependency container has been built.
    /// </summary>
    public void InitializeAll() {
        if (_initialized)
            return;

        ConfigureServices();
        _initialized = true;

        var log = services.Get<ILoggingService>();

        foreach (var plugin in PluginRegistry.GetLoadOrder()) {
            try {
                log.Info($"Loading plugin: {plugin.Name}");
                plugin.Initialize(new PluginContext(services, plugin.Name));
            }
            catch (Exception ex) {
                log.Error($"Plugin initialization failed: {plugin.Name}", ex);
                PluginRegistry.Unload(plugin);
            }
        }
    }

    /// <summary>
    /// Unloads all plugins in reverse dependency order during shutdown.
    /// </summary>
    public ValueTask ShutdownAsync() {
        var log = services.Get<ILoggingService>();

        foreach (var plugin in PluginRegistry.GetUnloadOrder()) {
            try {
                log.Info($"Unloading plugin: {plugin.Name}");
                PluginRegistry.Unload(plugin);
            }
            catch (Exception ex) {
                log.Error($"Plugin unload failed: {plugin.Name}", ex);
            }
        }

        return ValueTask.CompletedTask;
    }
}
