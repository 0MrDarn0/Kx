// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;
using Kx.Utility;

namespace Kx.Core.Plugin;

/// <summary>
/// Loads plugins for a runtime using injected discovery, compatibility, and instantiation components.
/// </summary>
public sealed class PluginLoaderService {
    private readonly string _pluginRoot;
    private readonly Func<string, PluginDiagnostics, Dictionary<string, PluginCatalogEntry>> _discoverPlugins;
    private readonly PluginCompatibilityPolicy _compatibilityPolicy;
    private readonly PluginDependencyResolver _dependencyResolver;
    private readonly PluginInstanceLoader _instanceLoader;
    private readonly PluginDiagnostics _diagnostics;
    private readonly Action<IPlugin> _unloadPlugin;

    /// <summary>
    /// Initializes a new plugin loader service.
    /// </summary>
    /// <param name="pluginRoot">The plugin root folder to scan.</param>
    /// <param name="discoverPlugins">The catalog discovery delegate.</param>
    /// <param name="registry">The registry service used for plugin state and unload behavior.</param>
    /// <param name="compatibilityPolicy">The compatibility policy for discovered manifests.</param>
    /// <param name="dependencyResolver">The dependency resolver for discovered manifests.</param>
    /// <param name="instanceLoader">The plugin instance loader used for assembly activation.</param>
    /// <param name="diagnostics">The diagnostics sink used during plugin loading.</param>
    /// <param name="unloadPlugin">The unload action used for loaded plugins.</param>
    public PluginLoaderService(
        string? pluginRoot = null,
        Func<string, PluginDiagnostics, Dictionary<string, PluginCatalogEntry>>? discoverPlugins = null,
        PluginRegistryService? registry = null,
        PluginCompatibilityPolicy? compatibilityPolicy = null,
        PluginDependencyResolver? dependencyResolver = null,
        PluginInstanceLoader? instanceLoader = null,
        PluginDiagnostics? diagnostics = null,
        Action<IPlugin>? unloadPlugin = null) {
        _diagnostics = diagnostics ?? new PluginDiagnostics();
        var registryService = registry ?? new PluginRegistryService();
        _pluginRoot = string.IsNullOrWhiteSpace(pluginRoot) ? Paths.PluginFolder : pluginRoot;
        _discoverPlugins = discoverPlugins ?? PluginDiscovery.Discover;
        _compatibilityPolicy = compatibilityPolicy ?? new PluginCompatibilityPolicy(diagnostics: _diagnostics);
        _dependencyResolver = dependencyResolver ?? new PluginDependencyResolver();
        _instanceLoader = instanceLoader ?? new PluginInstanceLoader(registerPlugin: registryService.Register, diagnostics: _diagnostics);
        _unloadPlugin = unloadPlugin ?? registryService.Unload;
    }

    /// <summary>
    /// Loads a named plugin and throws when it cannot be activated.
    /// </summary>
    /// <param name="name">The plugin name to load.</param>
    public T Load<T>(string name) where T : IPlugin {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Directory.Exists(_pluginRoot))
            throw new InvalidOperationException($"Plugin '{name}' not found.");

        var plugins = _discoverPlugins(_pluginRoot, _diagnostics);
        if (!plugins.TryGetValue(name, out var plugin))
            throw new InvalidOperationException($"Plugin '{name}' not found.");

        if (!_compatibilityPolicy.IsCompatible(plugin.Manifest))
            throw new InvalidOperationException($"Plugin '{name}' not found.");

        if (plugin.DllPath is null)
            throw new InvalidOperationException($"No DLL found for plugin '{name}' in '{plugin.Folder}'.");

        return _instanceLoader.LoadRequired<T>(plugin.Manifest, plugin.Folder, plugin.DllPath);
    }

    /// <summary>
    /// Loads all compatible plugins in dependency-resolved order.
    /// </summary>
    public IReadOnlyList<T> LoadAll<T>() where T : IPlugin {
        var result = new List<T>();

        if (!Directory.Exists(_pluginRoot)) {
            _diagnostics.Trace("PluginLoader", $"Plugin root '{_pluginRoot}' does not exist.");
            return result;
        }

        var plugins = _discoverPlugins(_pluginRoot, _diagnostics);
        if (plugins.Count == 0) {
            _diagnostics.Trace("PluginLoader", "No plugins discovered.");
            return result;
        }

        IReadOnlyList<string> loadOrder;
        try {
            loadOrder = _dependencyResolver.ResolveLoadOrder(plugins.ToDictionary(x => x.Key, x => x.Value.Manifest, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex) {
            _diagnostics.Trace("PluginLoader", $"Failed to resolve dependency graph: {ex.Message}");
            return result;
        }

        foreach (var name in loadOrder) {
            var plugin = plugins[name];

            if (!_compatibilityPolicy.IsCompatible(plugin.Manifest)) {
                _diagnostics.Trace("PluginLoader", $"Plugin '{plugin.Name}' skipped due to API mismatch.");
                continue;
            }

            if (plugin.DllPath is null) {
                _diagnostics.Trace("PluginLoader", $"Plugin '{plugin.Name}' has no DLL.");
                continue;
            }

            var instance = _instanceLoader.TryLoad<T>(plugin.Manifest, plugin.DllPath);
            if (instance is null)
                continue;

            result.Add(instance);
        }

        return result;
    }

    /// <summary>
    /// Unloads a previously loaded plugin.
    /// </summary>
    /// <param name="plugin">The plugin instance to unload.</param>
    public void Unload(IPlugin plugin) {
        ArgumentNullException.ThrowIfNull(plugin);
        _unloadPlugin(plugin);
    }
}
