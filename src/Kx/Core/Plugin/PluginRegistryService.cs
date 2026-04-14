// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Runtime.Loader;
using Kx.Sdk.Plugin;

namespace Kx.Core.Plugin;

/// <summary>
/// Stores loaded plugin instances together with their load contexts for one runtime.
/// </summary>
public sealed class PluginRegistryService {
    private sealed record Entry(IPlugin Plugin, AssemblyLoadContext Context);

    private readonly object _sync = new();
    private readonly Dictionary<IPlugin, Entry> _entries = new();
    private readonly List<IPlugin> _loadOrder = [];
    private readonly Action _collectGarbage;

    /// <summary>
    /// Initializes a new plugin registry service.
    /// </summary>
    /// <param name="collectGarbage">The cleanup action that runs after unloading a plugin context.</param>
    public PluginRegistryService(Action? collectGarbage = null) {
        _collectGarbage = collectGarbage ?? CollectGarbage;
    }

    /// <summary>
    /// Registers a plugin instance together with its load context.
    /// </summary>
    /// <param name="plugin">The plugin instance to register.</param>
    /// <param name="context">The load context that owns the plugin assembly.</param>
    public void Register(IPlugin plugin, AssemblyLoadContext context) {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        lock (_sync) {
            _entries[plugin] = new Entry(plugin, context);

            if (!_loadOrder.Contains(plugin))
                _loadOrder.Add(plugin);
        }
    }

    /// <summary>
    /// Tries to get the load context for a registered plugin.
    /// </summary>
    /// <param name="plugin">The plugin instance to look up.</param>
    /// <param name="context">The resolved load context when found.</param>
    public bool TryGetContext(IPlugin plugin, out AssemblyLoadContext? context) {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (_sync) {
            if (_entries.TryGetValue(plugin, out var entry)) {
                context = entry.Context;
                return true;
            }

            context = null;
            return false;
        }
    }

    /// <summary>
    /// Returns the currently loaded plugins in reverse unload order.
    /// </summary>
    public IReadOnlyList<IPlugin> GetUnloadOrder() {
        lock (_sync) {
            return _loadOrder
                .Where(_entries.ContainsKey)
                .Reverse()
                .ToArray();
        }
    }

    /// <summary>
    /// Returns the currently loaded plugins in dependency-resolved load order.
    /// </summary>
    public IReadOnlyList<IPlugin> GetLoadOrder() {
        lock (_sync) {
            return _loadOrder
                .Where(_entries.ContainsKey)
                .ToArray();
        }
    }

    /// <summary>
    /// Removes a plugin from the registry without unloading its context.
    /// </summary>
    /// <param name="plugin">The plugin instance to unregister.</param>
    public void Unregister(IPlugin plugin) {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (_sync) {
            _entries.Remove(plugin);
            _loadOrder.Remove(plugin);
        }
    }

    /// <summary>
    /// Unloads a plugin by disposing it, unloading its context, and cleaning up registry state.
    /// </summary>
    /// <param name="plugin">The plugin instance to unload.</param>
    public void Unload(IPlugin plugin) {
        ArgumentNullException.ThrowIfNull(plugin);

        var entry = Detach(plugin);
        if (entry is null)
            return;

        try {
            entry.Plugin.Dispose();
        }
        finally {
            entry.Context.Unload();
            _collectGarbage();
        }
    }

    private Entry? Detach(IPlugin plugin) {
        lock (_sync) {
            if (!_entries.TryGetValue(plugin, out var entry))
                return null;

            _entries.Remove(plugin);
            _loadOrder.Remove(plugin);
            return entry;
        }
    }

    private static void CollectGarbage() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
