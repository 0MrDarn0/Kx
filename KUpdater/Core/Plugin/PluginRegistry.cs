// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Plugin;

namespace KUpdater.Core.Plugin;

public static class PluginRegistry {
    private static readonly Dictionary<IPlugin, PluginLoadContext> _contexts = [];

    public static void Register(IPlugin plugin, PluginLoadContext context)
        => _contexts[plugin] = context;

    public static bool TryGetContext(IPlugin plugin, out PluginLoadContext? context)
        => _contexts.TryGetValue(plugin, out context);

    public static void Unregister(IPlugin plugin)
        => _contexts.Remove(plugin);

    public static IEnumerable<IPlugin> LoadedPlugins => _contexts.Keys;
}
