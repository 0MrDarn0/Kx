// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using System.Runtime.Loader;
using Kx.Sdk.Plugin;

namespace Kx.Core.Plugin;

/// <summary>
/// Loads plugin instances from compiled plugin assemblies and registers them with the runtime registry.
/// </summary>
public sealed class PluginInstanceLoader {
    private readonly Func<string, AssemblyLoadContext> _createLoadContext;
    private readonly Func<AssemblyLoadContext, string, Assembly> _loadAssembly;
    private readonly Action<IPlugin, AssemblyLoadContext> _registerPlugin;
    private readonly Action<AssemblyLoadContext> _unloadLoadContext;
    private readonly PluginDiagnostics _diagnostics;

    /// <summary>
    /// Initializes a new plugin instance loader.
    /// </summary>
    /// <param name="createLoadContext">Creates the load context used for a plugin assembly path.</param>
    /// <param name="loadAssembly">Loads an assembly from the specified path into the provided load context.</param>
    /// <param name="registerPlugin">Registers a created plugin instance with its load context.</param>
    /// <param name="unloadLoadContext">Unloads a load context after a failed load attempt.</param>
    public PluginInstanceLoader(
        Func<string, AssemblyLoadContext>? createLoadContext = null,
        Func<AssemblyLoadContext, string, Assembly>? loadAssembly = null,
        Action<IPlugin, AssemblyLoadContext>? registerPlugin = null,
        Action<AssemblyLoadContext>? unloadLoadContext = null,
        PluginDiagnostics? diagnostics = null) {
        _createLoadContext = createLoadContext ?? (path => new PluginLoadContext(path));
        _loadAssembly = loadAssembly ?? ((context, path) => context.LoadFromAssemblyPath(path));
        _registerPlugin = registerPlugin ?? PluginRegistry.Register;
        _unloadLoadContext = unloadLoadContext ?? (context => context.Unload());
        _diagnostics = diagnostics ?? new PluginDiagnostics();
    }

    /// <summary>
    /// Attempts to load a plugin instance and returns <see langword="null" /> when the assembly or entry type is invalid.
    /// </summary>
    /// <param name="manifest">The plugin manifest describing the entry type to load.</param>
    /// <param name="dllPath">The plugin assembly path.</param>
    public T? TryLoad<T>(PluginManifest manifest, string? dllPath) where T : IPlugin {
        ArgumentNullException.ThrowIfNull(manifest);

        if (string.IsNullOrWhiteSpace(dllPath))
            return default;

        var loadContext = _createLoadContext(dllPath);

        try {
            var assembly = _loadAssembly(loadContext, dllPath);
            var pluginType = assembly.GetType(manifest.EntryType);
            if (pluginType is null) {
                _diagnostics.Trace("PluginLoader", $"EntryType '{manifest.EntryType}' not found in '{dllPath}'.");
                Unload(loadContext);
                return default;
            }

            if (!typeof(T).IsAssignableFrom(pluginType)) {
                _diagnostics.Trace("PluginLoader", $"EntryType '{manifest.EntryType}' does not implement {typeof(T).Name}.");
                Unload(loadContext);
                return default;
            }

            return CreateInstance<T>(manifest, pluginType, loadContext, required: false);
        }
        catch (Exception ex) {
            _diagnostics.Trace("PluginLoader", $"Failed to load assembly '{dllPath}': {ex.Message}");
            Unload(loadContext);
            return default;
        }
    }

    /// <summary>
    /// Loads a required plugin instance and throws when the assembly or entry type is invalid.
    /// </summary>
    /// <param name="manifest">The plugin manifest describing the entry type to load.</param>
    /// <param name="folder">The plugin folder used for diagnostics.</param>
    /// <param name="dllPath">The plugin assembly path.</param>
    public T LoadRequired<T>(PluginManifest manifest, string folder, string? dllPath) where T : IPlugin {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        if (string.IsNullOrWhiteSpace(dllPath))
            throw new InvalidOperationException($"No DLL found for plugin '{manifest.Name}' in '{folder}'.");

        var loadContext = _createLoadContext(dllPath);

        try {
            var assembly = _loadAssembly(loadContext, dllPath);
            var pluginType = assembly.GetType(manifest.EntryType)
                ?? throw new InvalidOperationException($"EntryType '{manifest.EntryType}' not found in plugin '{manifest.Name}'.");

            if (!typeof(T).IsAssignableFrom(pluginType))
                throw new InvalidOperationException($"Plugin '{manifest.Name}' entry type '{manifest.EntryType}' does not implement {typeof(T).Name}.");

            return CreateInstance<T>(manifest, pluginType, loadContext, required: true)!;
        }
        catch (Exception ex) {
            _diagnostics.Error("PluginLoader", $"Failed to load assembly '{dllPath}'", ex);
            Unload(loadContext);
            throw;
        }
    }

    private T? CreateInstance<T>(PluginManifest manifest, Type pluginType, AssemblyLoadContext loadContext, bool required) where T : IPlugin {
        try {
            var instance = (T)Activator.CreateInstance(pluginType)!;
            _registerPlugin(instance, loadContext);
            return instance;
        }
        catch (Exception ex) {
            if (!required) {
                _diagnostics.Trace("PluginLoader", $"Failed to instantiate plugin '{manifest.Name}': {ex.Message}");
                Unload(loadContext);
            }

            if (required)
                throw;

            return default;
        }
    }

    private void Unload(AssemblyLoadContext loadContext) {
        _unloadLoadContext(loadContext);
    }
}
