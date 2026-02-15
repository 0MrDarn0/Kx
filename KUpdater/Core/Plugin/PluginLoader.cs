// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using KUpdater.Abstractions.Plugin;
using KUpdater.Utility;

namespace KUpdater.Core.Plugin;

public static class PluginLoader {
    public static T Load<T>(string name) where T : IPlugin {
        var pluginRoot = Paths.PluginFolder;

        foreach (var folder in Directory.GetDirectories(pluginRoot)) {
            var manifestPath = Path.Combine(folder, "plugin.yaml");
            if (!File.Exists(manifestPath))
                continue;

            PluginManifest manifest;
            try {
                manifest = PluginManifestLoader.Load(manifestPath);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Invalid manifest in '{folder}': {ex}");
                continue;
            }

            if (!manifest.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            var dllPath = Directory.GetFiles(folder, "*.dll").FirstOrDefault();
            if (dllPath is null) {
                throw new InvalidOperationException($"No DLL found for plugin '{name}' in '{folder}'.");
            }

            var alc = new PluginLoadContext(dllPath);
            Assembly assembly;
            try {
                assembly = alc.LoadFromAssemblyPath(dllPath);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Failed to load assembly '{dllPath}': {ex}");
                alc.Unload();
                throw;
            }

            var type = assembly.GetType(manifest.EntryType)
                       ?? throw new InvalidOperationException(
                           $"EntryType '{manifest.EntryType}' not found in plugin '{name}'.");

            if (!typeof(T).IsAssignableFrom(type)) {
                throw new InvalidOperationException(
                    $"Plugin '{name}' entry type '{manifest.EntryType}' does not implement {typeof(T).Name}.");
            }

            var instance = (T)Activator.CreateInstance(type)!;
            PluginRegistry.Register(instance, alc);
            return instance;
        }

        throw new InvalidOperationException($"Plugin '{name}' not found.");
    }

    public static void Unload(IPlugin plugin) {
        if (!PluginRegistry.TryGetContext(plugin, out var alc) || alc is null)
            return;

        (plugin as IDisposable)?.Dispose();
        PluginRegistry.Unregister(plugin);
        alc.Unload();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
