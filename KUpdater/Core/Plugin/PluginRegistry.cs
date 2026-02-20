// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using System.Runtime.Loader;
using KUpdater.Abstractions.Plugin;

namespace KUpdater.Core.Plugin;

public static class PluginRegistry {
    private static readonly ConcurrentDictionary<IPlugin, AssemblyLoadContext> _contexts = new();

    public static void Register(IPlugin plugin, AssemblyLoadContext context) {
        _contexts[plugin] = context;
    }

    public static bool TryGetContext(IPlugin plugin, out AssemblyLoadContext? context) {
        return _contexts.TryGetValue(plugin, out context);
    }

    public static void Unregister(IPlugin plugin) {
        _contexts.TryRemove(plugin, out _);
    }
}
