// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Runtime.Loader;
using Kx.Sdk.Plugin;

namespace Kx.Core.Plugin;

public static class PluginRegistry {
    private static readonly PluginRegistryService _registry = new();

    public static void Register(IPlugin plugin, AssemblyLoadContext context) {
        _registry.Register(plugin, context);
    }

    public static bool TryGetContext(IPlugin plugin, out AssemblyLoadContext? context) {
        return _registry.TryGetContext(plugin, out context);
    }

    public static IReadOnlyList<IPlugin> GetUnloadOrder() {
        return _registry.GetUnloadOrder();
    }

    /// <summary>
    /// Returns the currently loaded plugins in dependency-resolved load order.
    /// </summary>
    public static IReadOnlyList<IPlugin> GetLoadOrder() {
        return _registry.GetLoadOrder();
    }

    public static void Unregister(IPlugin plugin) {
        _registry.Unregister(plugin);
    }

    public static void Unload(IPlugin plugin) {
        _registry.Unload(plugin);
    }
}
