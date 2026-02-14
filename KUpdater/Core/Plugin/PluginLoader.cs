// Copyright (c) 2026 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using KUpdater.Abstractions.Plugin;
using KUpdater.Utility;

namespace KUpdater.Core.Plugin;

public static class PluginLoader {
    public static T Load<T>(string name) where T : IPlugin {
        foreach (var dll in Directory.GetFiles(Paths.PluginFolder, "*.dll")) {
            var alc = new PluginLoadContext(dll);

            Assembly assembly;
            try {
                assembly = alc.LoadFromAssemblyPath(dll);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Failed to load plugin assembly '{dll}': {ex}");
                alc.Unload();
                continue;
            }

            var pluginTypes = assembly.GetTypes()
                .Where(t =>
                    typeof(T).IsAssignableFrom(t) &&
                    !t.IsInterface &&
                    !t.IsAbstract);

            foreach (var type in pluginTypes) {
                try {
                    var instance = (T)Activator.CreateInstance(type)!;

                    if (instance.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                        PluginRegistry.Register(instance, alc);
                        return instance;
                    }
                }
                catch (Exception ex) {
                    Console.Error.WriteLine($"Failed to instantiate plugin '{type.FullName}': {ex}");
                }
            }

            // Kein Plugin gefunden → entladen
            alc.Unload();
        }

        throw new Exception($"Plugin '{name}' not found.");
    }

    public static void Unload(IPlugin plugin) {
        if (!PluginRegistry.TryGetContext(plugin, out var alc))
            return;

        (plugin as IDisposable)?.Dispose();

        PluginRegistry.Unregister(plugin);

        alc?.Unload();

        // GC forcieren, damit der ALC wirklich entladen wird
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
