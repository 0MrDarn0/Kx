// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Reflection;
using KUpdater.Abstractions.Plugin;
using KUpdater.Utility;

namespace KUpdater.Core.Plugin;

public static class PluginLoader {
    private sealed record PluginInfo(
        string Name,
        string Folder,
        PluginManifest Manifest,
        string? DllPath
    );

    public static T Load<T>(string name) where T : IPlugin {
        var pluginRoot = Paths.PluginFolder;

        foreach (var folder in Directory.GetDirectories(pluginRoot)) {
            var manifestPath = Path.Combine(folder, "KPlugin.yaml");
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

        if (!Directory.Exists(pluginRoot)) {
            Debug.WriteLine($"[PluginLoader] Plugin root '{pluginRoot}' does not exist.");
            return result;
        }

        // 1) Alle Manifeste + DLLs einsammeln
        var plugins = DiscoverPlugins(pluginRoot);

        if (plugins.Count == 0) {
            Debug.WriteLine("[PluginLoader] No plugins discovered.");
            return result;
        }

        // 2) Lade-Reihenfolge per Dependencies bestimmen
        List<string> loadOrder;
        try {
            loadOrder = ResolveLoadOrder(plugins);
        }
        catch (Exception ex) {
            Debug.WriteLine($"[PluginLoader] Failed to resolve dependency graph: {ex.Message}");
            return result;
        }

        // 3) In Reihenfolge laden
        foreach (var name in loadOrder) {
            var info = plugins[name];

            if (!IsApiCompatible(info.Manifest)) {
                Debug.WriteLine($"[PluginLoader] Plugin '{info.Name}' skipped due to API mismatch.");
                continue;
            }

            if (info.DllPath is null) {
                Debug.WriteLine($"[PluginLoader] Plugin '{info.Name}' has no DLL.");
                continue;
            }

            var instance = LoadPluginInstance<T>(info);
            if (instance is null)
                continue;

            result.Add(instance);
        }

        return result;
    }

    // ---------------- intern ----------------

    private static Dictionary<string, PluginInfo> DiscoverPlugins(string pluginRoot) {
        var dict = new Dictionary<string, PluginInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in Directory.GetDirectories(pluginRoot)) {
            var manifestPath = Path.Combine(folder, "KPlugin.yaml");
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

            var dllPath = Directory.GetFiles(folder, "*.dll").FirstOrDefault();
            if (dllPath is null) {
                Debug.WriteLine($"[PluginLoader] No DLL found in '{folder}' for plugin '{manifest.Name}'.");
            }

            var info = new PluginInfo(
                manifest.Name,
                folder,
                manifest,
                dllPath
            );

            dict[manifest.Name] = info;
        }

        return dict;
    }

    private static List<string> ResolveLoadOrder(Dictionary<string, PluginInfo> plugins) {
        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(string name) {
            if (visited.Contains(name))
                return;

            if (!plugins.ContainsKey(name))
                throw new InvalidOperationException($"Unknown plugin '{name}' in dependency graph.");

            if (visiting.Contains(name))
                throw new InvalidOperationException($"Cyclic dependency detected at '{name}'.");

            visiting.Add(name);

            foreach (var dep in plugins[name].Manifest.Dependencies) {
                if (!plugins.ContainsKey(dep)) {
                    throw new InvalidOperationException(
                        $"Missing dependency '{dep}' for plugin '{name}'.");
                }

                Visit(dep);
            }

            visiting.Remove(name);
            visited.Add(name);
            result.Add(name);
        }

        foreach (var name in plugins.Keys)
            Visit(name);

        return result;
    }

    private static bool IsApiCompatible(PluginManifest manifest) {
        if (!Version.TryParse(manifest.ApiVersion, out var pluginApi) ||
            !Version.TryParse(HostInfo.ApiVersion, out var hostApi)) {
            Debug.WriteLine($"[PluginLoader] Invalid API version format in plugin '{manifest.Name}'.");
            return false;
        }

        if (pluginApi.Major != hostApi.Major) {
            Debug.WriteLine($"[PluginLoader] Plugin '{manifest.Name}' incompatible major API {pluginApi} vs host {hostApi}.");
            return false;
        }

        if (pluginApi.Minor > hostApi.Minor) {
            Debug.WriteLine($"[PluginLoader] Plugin '{manifest.Name}' requires newer API {pluginApi} than host {hostApi}.");
            return false;
        }

        return true;
    }

    private static T? LoadPluginInstance<T>(PluginInfo info) where T : IPlugin {
        if (info.DllPath is null)
            return default;

        var alc = new PluginLoadContext(info.DllPath);
        Assembly assembly;

        try {
            assembly = alc.LoadFromAssemblyPath(info.DllPath);
        }
        catch (Exception ex) {
            Debug.WriteLine($"[PluginLoader] Failed to load assembly '{info.DllPath}': {ex.Message}");
            alc.Unload();
            return default;
        }

        var type = assembly.GetType(info.Manifest.EntryType);
        if (type is null) {
            Debug.WriteLine($"[PluginLoader] EntryType '{info.Manifest.EntryType}' not found in '{info.DllPath}'.");
            alc.Unload();
            return default;
        }

        if (!typeof(T).IsAssignableFrom(type)) {
            Debug.WriteLine($"[PluginLoader] EntryType '{info.Manifest.EntryType}' does not implement {typeof(T).Name}.");
            alc.Unload();
            return default;
        }

        try {
            var instance = (T)Activator.CreateInstance(type)!;
            PluginRegistry.Register(instance, alc);
            return instance;
        }
        catch (Exception ex) {
            Debug.WriteLine($"[PluginLoader] Failed to instantiate plugin '{info.Manifest.Name}': {ex.Message}");
            alc.Unload();
            return default;
        }
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
