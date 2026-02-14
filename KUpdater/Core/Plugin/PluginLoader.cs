// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using KUpdater.Abstractions.Plugin;

namespace KUpdater.Core.Plugin;

public static class PluginLoader {
    public static T Load<T>(string name) where T : IPlugin {
        string pluginDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Plugins");

        foreach (var dll in Directory.GetFiles(pluginDirectory, "*.dll")) {
            var assembly = Assembly.LoadFrom(dll);

            var pluginTypes = assembly.GetTypes()
                .Where(t =>
                    typeof(T).IsAssignableFrom(t) &&
                    !t.IsInterface &&
                    !t.IsAbstract);

            foreach (var type in pluginTypes) {
                var instance = (T)Activator.CreateInstance(type)!;

                if (instance.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return instance;
            }
        }

        throw new Exception($"Plugin '{name}' not found.");
    }
}
