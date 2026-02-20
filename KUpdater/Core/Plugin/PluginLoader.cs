// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
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

            if (!Version.TryParse(manifest.ApiVersion, out var pluginApi) ||
                !Version.TryParse(HostInfo.ApiVersion, out var hostApi)) {
                Debug.WriteLine($"[PluginLoader] Invalid API version format in '{manifest.Name}'.");
                continue;
            }

            if (pluginApi.Major != hostApi.Major) {
                Debug.WriteLine($"[PluginLoader] Plugin '{manifest.Name}' incompatible major API version {pluginApi} vs {hostApi}. Skipping.");
                continue;
            }

            if (pluginApi.Minor > hostApi.Minor) {
                Debug.WriteLine($"[PluginLoader] Plugin '{manifest.Name}' requires newer API {pluginApi} than host {hostApi}. Skipping.");
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

    public static IReadOnlyList<T> LoadAll<T>() where T : IPlugin {
        var pluginRoot = Paths.PluginFolder;
        var result = new List<T>();

        foreach (var folder in Directory.GetDirectories(pluginRoot)) {
            var manifestPath = Path.Combine(folder, "plugin.yaml");
            if (!File.Exists(manifestPath))
                continue;

            PluginManifest manifest;
            try {
                manifest = PluginManifestLoader.Load(manifestPath);
            }
            catch (Exception ex) {
                Debug.WriteLine($"[PluginLoader] Invalid manifest in '{folder}': {ex.Message}");
                continue;
            }

            if (!Version.TryParse(manifest.ApiVersion, out var pluginApi) ||
                !Version.TryParse(HostInfo.ApiVersion, out var hostApi)) {
                Debug.WriteLine($"[PluginLoader] Invalid API version format in '{manifest.Name}'.");
                continue;
            }

            if (pluginApi.Major != hostApi.Major) {
                Debug.WriteLine($"[PluginLoader] Plugin '{manifest.Name}' incompatible major API version {pluginApi} vs {hostApi}. Skipping.");
                continue;
            }

            if (pluginApi.Minor > hostApi.Minor) {
                Debug.WriteLine($"[PluginLoader] Plugin '{manifest.Name}' requires newer API {pluginApi} than host {hostApi}. Skipping.");
                continue;
            }

            var dllPath = Directory.GetFiles(folder, "*.dll").FirstOrDefault();
            if (dllPath is null) {
                Debug.WriteLine($"[PluginLoader] No DLL found in '{folder}'.");
                continue;
            }

            var alc = new PluginLoadContext(dllPath);
            Assembly assembly;

            try {
                assembly = alc.LoadFromAssemblyPath(dllPath);
            }
            catch (Exception ex) {
                Debug.WriteLine($"[PluginLoader] Failed to load assembly '{dllPath}': {ex.Message}");
                alc.Unload();
                continue;
            }

            var type = assembly.GetType(manifest.EntryType);
            if (type is null) {
                Debug.WriteLine($"[PluginLoader] EntryType '{manifest.EntryType}' not found in '{dllPath}'.");
                alc.Unload();
                continue;
            }

            if (!typeof(T).IsAssignableFrom(type)) {
                Debug.WriteLine($"[PluginLoader] EntryType '{manifest.EntryType}' does not implement {typeof(T).Name}.");
                alc.Unload();
                continue;
            }

            try {
                var instance = (T)Activator.CreateInstance(type)!;
                PluginRegistry.Register(instance, alc);
                result.Add(instance);
            }
            catch (Exception ex) {
                Debug.WriteLine($"[PluginLoader] Failed to instantiate plugin '{manifest.Name}': {ex.Message}");
                alc.Unload();
            }
        }

        return result;
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
