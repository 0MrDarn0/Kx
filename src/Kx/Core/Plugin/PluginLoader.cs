// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;

namespace Kx.Core.Plugin;

public static class PluginLoader {
    private static readonly PluginLoaderService _loader = new();

    public static T Load<T>(string name) where T : IPlugin {
        return _loader.Load<T>(name);
    }

    public static IReadOnlyList<T> LoadAll<T>() where T : IPlugin {
        return _loader.LoadAll<T>();
    }

    public static void Unload(IPlugin plugin) {
        _loader.Unload(plugin);
    }
}
