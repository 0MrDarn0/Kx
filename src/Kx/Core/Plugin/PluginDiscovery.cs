// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Plugin;

internal static class PluginDiscovery {
    public static Dictionary<string, PluginCatalogEntry> Discover(string pluginRoot, PluginDiagnostics diagnostics) {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginRoot);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var plugins = new Dictionary<string, PluginCatalogEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in Directory.GetDirectories(pluginRoot)) {
            var manifestPath = Path.Combine(folder, "Plugin.yaml");
            if (!File.Exists(manifestPath))
                continue;

            PluginManifest manifest;
            try {
                manifest = PluginManifestLoader.Load(manifestPath);
            }
            catch (Exception ex) {
                diagnostics.Trace("PluginLoader", $"Invalid manifest in '{folder}': {ex.Message}");
                continue;
            }

            var dllPath = Directory.GetFiles(folder, "*.dll").FirstOrDefault();
            if (dllPath is null)
                diagnostics.Trace("PluginLoader", $"No DLL found in '{folder}' for plugin '{manifest.Name}'.");

            plugins[manifest.Name] = new PluginCatalogEntry(
                manifest.Name,
                folder,
                manifest,
                dllPath);
        }

        return plugins;
    }
}
