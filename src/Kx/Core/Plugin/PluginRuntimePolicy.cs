// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Logging;
using Kx.Sdk.Plugin;

namespace Kx.Core.Plugin;

/// <summary>
/// Encapsulates plugin loading, ordering, and fault handling policy for the runtime.
/// </summary>
public sealed class PluginRuntimePolicy {
    private readonly Func<IReadOnlyList<IPlugin>> _loadPlugins;
    private readonly Func<IReadOnlyList<IPlugin>> _getLoadOrder;
    private readonly Func<IReadOnlyList<IPlugin>> _getUnloadOrder;
    private readonly Action<IPlugin> _unloadPlugin;
    private readonly PluginDiagnostics _diagnostics;

    /// <summary>
    /// Initializes a new plugin runtime policy.
    /// </summary>
    /// <param name="loader">The loader service used for plugin discovery and unload operations.</param>
    /// <param name="registry">The registry service used for load and unload order lookups.</param>
    /// <param name="loadPlugins">Provides the plugins to load before service registration.</param>
    /// <param name="getLoadOrder">Provides the current plugin initialization order.</param>
    /// <param name="getUnloadOrder">Provides the current plugin shutdown order.</param>
    /// <param name="unloadPlugin">Unloads a plugin instance after a failure or during shutdown.</param>
    public PluginRuntimePolicy(
        PluginLoaderService? loader = null,
        PluginRegistryService? registry = null,
        Func<IReadOnlyList<IPlugin>>? loadPlugins = null,
        Func<IReadOnlyList<IPlugin>>? getLoadOrder = null,
        Func<IReadOnlyList<IPlugin>>? getUnloadOrder = null,
        Action<IPlugin>? unloadPlugin = null,
        PluginDiagnostics? diagnostics = null) {
        var diagnosticsService = diagnostics ?? new PluginDiagnostics();
        var registryService = registry ?? new PluginRegistryService();
        var loaderService = loader ?? new PluginLoaderService(registry: registryService, diagnostics: diagnosticsService);

        _loadPlugins = loadPlugins ?? loaderService.LoadAll<IPlugin>;
        _getLoadOrder = getLoadOrder ?? registryService.GetLoadOrder;
        _getUnloadOrder = getUnloadOrder ?? registryService.GetUnloadOrder;
        _unloadPlugin = unloadPlugin ?? loaderService.Unload;
        _diagnostics = diagnosticsService;
    }

    /// <summary>
    /// Loads plugins and lets service plugins register services before the container is built.
    /// </summary>
    /// <param name="services">The service registry used by the host.</param>
    public void ConfigureServices(IServiceRegistry services) {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var plugin in _loadPlugins()) {
            if (plugin is not IServicePlugin servicePlugin)
                continue;

            try {
                _diagnostics.Trace("PluginManager", $"Configuring services for plugin: {plugin.Name}");
                servicePlugin.ConfigureServices(services);
            }
            catch (Exception ex) {
                _diagnostics.Trace("PluginManager", $"Service registration failed for plugin '{plugin.Name}': {ex}");
                _unloadPlugin(plugin);
            }
        }
    }

    /// <summary>
    /// Initializes loaded plugins in dependency-resolved order.
    /// </summary>
    /// <param name="services">The built dependency container.</param>
    /// <param name="log">The logging service used for plugin lifecycle messages.</param>
    public void InitializePlugins(IDependencyContainer services, ILoggingService log) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(log);

        foreach (var plugin in _getLoadOrder()) {
            try {
                log.Info($"Loading plugin: {plugin.Name}");
                plugin.Initialize(new PluginContext(services, plugin.Name));
            }
            catch (Exception ex) {
                log.Error($"Plugin initialization failed: {plugin.Name}", ex);
                _unloadPlugin(plugin);
            }
        }
    }

    /// <summary>
    /// Unloads plugins in reverse dependency order during shutdown.
    /// </summary>
    /// <param name="log">The logging service used for plugin lifecycle messages.</param>
    public void ShutdownPlugins(ILoggingService log) {
        ArgumentNullException.ThrowIfNull(log);

        foreach (var plugin in _getUnloadOrder()) {
            try {
                log.Info($"Unloading plugin: {plugin.Name}");
                _unloadPlugin(plugin);
            }
            catch (Exception ex) {
                log.Error($"Plugin unload failed: {plugin.Name}", ex);
            }
        }
    }
}
