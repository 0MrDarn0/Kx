using System.Runtime.Loader;
using Kx.Core.Plugin;
using Kx.Sdk.Plugin;

namespace Kx.Tests;

public sealed class PluginLoaderServiceTests {
    [Fact]
    public void WhenNamedPluginExistsThenLoadReturnsPluginInstance() {
        var diagnostics = new PluginDiagnostics(traceWriter: _ => { }, errorWriter: _ => { });
        var service = new PluginLoaderService(
            pluginRoot: AppContext.BaseDirectory,
            discoverPlugins: (_, _) => new Dictionary<string, PluginCatalogEntry>(StringComparer.OrdinalIgnoreCase) {
                ["Sample"] = CreateCatalogEntry("Sample", typeof(SamplePlugin))
            },
            instanceLoader: CreateInstanceLoader(diagnostics),
            diagnostics: diagnostics);

        var plugin = service.Load<IPlugin>("Sample");

        Assert.IsType<SamplePlugin>(plugin);
    }

    [Fact]
    public void WhenLoadingAllThenDependencyOrderIsPreserved() {
        var diagnostics = new PluginDiagnostics(traceWriter: _ => { }, errorWriter: _ => { });
        var service = new PluginLoaderService(
            pluginRoot: AppContext.BaseDirectory,
            discoverPlugins: (_, _) => new Dictionary<string, PluginCatalogEntry>(StringComparer.OrdinalIgnoreCase) {
                ["Shell"] = CreateCatalogEntry("Shell", typeof(ShellPlugin), ["Core"]),
                ["Core"] = CreateCatalogEntry("Core", typeof(CorePlugin))
            },
            instanceLoader: CreateInstanceLoader(diagnostics),
            diagnostics: diagnostics);

        var plugins = service.LoadAll<IPlugin>();

        Assert.Equal(["Core", "Shell"], plugins.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void WhenUnloadIsCalledThenConfiguredUnloadActionRuns() {
        var diagnostics = new PluginDiagnostics(traceWriter: _ => { }, errorWriter: _ => { });
        var plugin = new SamplePlugin();
        IPlugin? unloadedPlugin = null;
        var service = new PluginLoaderService(
            pluginRoot: AppContext.BaseDirectory,
            diagnostics: diagnostics,
            unloadPlugin: candidate => unloadedPlugin = candidate);

        service.Unload(plugin);

        Assert.Same(plugin, unloadedPlugin);
    }

    private static PluginCatalogEntry CreateCatalogEntry(string name, Type pluginType, List<string>? dependencies = null) {
        return new PluginCatalogEntry(
            name,
            $"Plugins\\{name}",
            new PluginManifest {
                Name = name,
                ApiVersion = global::Kx.Core.HostInfo.ApiVersion,
                EntryType = pluginType.FullName!,
                Dependencies = dependencies ?? []
            },
            $"{name}.dll");
    }

    private static PluginInstanceLoader CreateInstanceLoader(PluginDiagnostics diagnostics) {
        return new PluginInstanceLoader(
            createLoadContext: _ => AssemblyLoadContext.Default,
            loadAssembly: static (_, _) => typeof(SamplePlugin).Assembly,
            unloadLoadContext: static _ => { },
            diagnostics: diagnostics);
    }

    private sealed class SamplePlugin : IPlugin {
        public string Name => "Sample";

        public void Initialize(IPluginContext context) {
        }

        public void Dispose() {
        }
    }

    private sealed class CorePlugin : IPlugin {
        public string Name => "Core";

        public void Initialize(IPluginContext context) {
        }

        public void Dispose() {
        }
    }

    private sealed class ShellPlugin : IPlugin {
        public string Name => "Shell";

        public void Initialize(IPluginContext context) {
        }

        public void Dispose() {
        }
    }
}
