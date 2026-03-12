// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Runtime.Loader;
using Kx.Abstractions.Plugin;

namespace Kx.Core.Plugin;

public static class PluginRegistry {
    private sealed record Entry(IPlugin Plugin, AssemblyLoadContext Context);

    private static readonly object _sync = new();
    private static readonly Dictionary<IPlugin, Entry> _entries = new();
    private static readonly List<IPlugin> _loadOrder = new();

    public static void Register(IPlugin plugin, AssemblyLoadContext context) {
        lock (_sync) {
            _entries[plugin] = new Entry(plugin, context);

            if (!_loadOrder.Contains(plugin))
                _loadOrder.Add(plugin);
        }
    }

    public static bool TryGetContext(IPlugin plugin, out AssemblyLoadContext? context) {
        lock (_sync) {
            if (_entries.TryGetValue(plugin, out var entry)) {
                context = entry.Context;
                return true;
            }

            context = null;
            return false;
        }
    }

    public static IReadOnlyList<IPlugin> GetUnloadOrder() {
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
    public static IReadOnlyList<IPlugin> GetLoadOrder() {
        lock (_sync) {
            return _loadOrder
                .Where(_entries.ContainsKey)
                .ToArray();
        }
    }

    public static void Unregister(IPlugin plugin) {
        lock (_sync) {
            _entries.Remove(plugin);
            _loadOrder.Remove(plugin);
        }
    }

    public static void Unload(IPlugin plugin) {
        var entry = Detach(plugin);
        if (entry is null)
            return;

        try {
            entry.Plugin.Dispose();
        }
        finally {
            entry.Context.Unload();
            CollectGarbage();
        }
    }

    private static Entry? Detach(IPlugin plugin) {
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
