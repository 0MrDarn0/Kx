// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Plugin;

/// <summary>
/// Builds the shared plugin runtime services for one runtime instance.
/// </summary>
public sealed class PluginRuntimeComposition {
    /// <summary>
    /// Initializes a new plugin runtime composition.
    /// </summary>
    /// <param name="pluginRoot">The plugin root folder used by the loader service.</param>
    /// <param name="discoverPlugins">The plugin catalog discovery delegate.</param>
    /// <param name="diagnostics">The diagnostics sink shared by plugin runtime components.</param>
    /// <param name="registry">The registry service shared by plugin runtime components.</param>
    /// <param name="compatibilityPolicy">The compatibility policy used by the loader service.</param>
    /// <param name="dependencyResolver">The dependency resolver used by the loader service.</param>
    /// <param name="instanceLoader">The plugin instance loader used by the loader service.</param>
    public PluginRuntimeComposition(
        string? pluginRoot = null,
        Func<string, PluginDiagnostics, Dictionary<string, PluginCatalogEntry>>? discoverPlugins = null,
        PluginDiagnostics? diagnostics = null,
        PluginRegistryService? registry = null,
        PluginCompatibilityPolicy? compatibilityPolicy = null,
        PluginDependencyResolver? dependencyResolver = null,
        PluginInstanceLoader? instanceLoader = null) {
        Diagnostics = diagnostics ?? new PluginDiagnostics();
        Registry = registry ?? new PluginRegistryService();
        CompatibilityPolicy = compatibilityPolicy ?? new PluginCompatibilityPolicy(diagnostics: Diagnostics);
        DependencyResolver = dependencyResolver ?? new PluginDependencyResolver();
        InstanceLoader = instanceLoader ?? new PluginInstanceLoader(registerPlugin: Registry.Register, diagnostics: Diagnostics);
        Loader = new PluginLoaderService(
            pluginRoot: pluginRoot,
            discoverPlugins: discoverPlugins,
            registry: Registry,
            compatibilityPolicy: CompatibilityPolicy,
            dependencyResolver: DependencyResolver,
            instanceLoader: InstanceLoader,
            diagnostics: Diagnostics);
        Policy = new PluginRuntimePolicy(
            loader: Loader,
            registry: Registry,
            diagnostics: Diagnostics);
    }

    /// <summary>
    /// Gets the diagnostics sink shared by the plugin runtime.
    /// </summary>
    public PluginDiagnostics Diagnostics { get; }

    /// <summary>
    /// Gets the registry service shared by the plugin runtime.
    /// </summary>
    public PluginRegistryService Registry { get; }

    /// <summary>
    /// Gets the compatibility policy shared by the plugin runtime.
    /// </summary>
    public PluginCompatibilityPolicy CompatibilityPolicy { get; }

    /// <summary>
    /// Gets the dependency resolver shared by the plugin runtime.
    /// </summary>
    public PluginDependencyResolver DependencyResolver { get; }

    /// <summary>
    /// Gets the instance loader shared by the plugin runtime.
    /// </summary>
    public PluginInstanceLoader InstanceLoader { get; }

    /// <summary>
    /// Gets the loader service shared by the plugin runtime.
    /// </summary>
    public PluginLoaderService Loader { get; }

    /// <summary>
    /// Gets the runtime policy shared by the plugin runtime.
    /// </summary>
    public PluginRuntimePolicy Policy { get; }
}
