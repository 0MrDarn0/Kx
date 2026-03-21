using System.Runtime.Loader;
using Kx.Core.Plugin;
using Kx.Sdk.Plugin;

namespace Kx.Tests;

public sealed class PluginInstanceLoaderTests {
    [Fact]
    public void WhenManifestResolvesPluginTypeThenTryLoadReturnsPluginInstance() {
        var registeredPlugins = new List<IPlugin>();
        var loader = CreateLoader(registeredPlugins);
        var manifest = new PluginManifest {
            Name = "Sample",
            EntryType = typeof(TestPlugin).FullName!
        };

        var instance = loader.TryLoad<IPlugin>(manifest, "sample.dll");

        Assert.IsType<TestPlugin>(instance);
    }

    [Fact]
    public void WhenManifestTypeDoesNotImplementPluginContractThenTryLoadReturnsNull() {
        var registeredPlugins = new List<IPlugin>();
        var loader = CreateLoader(registeredPlugins);
        var manifest = new PluginManifest {
            Name = "Broken",
            EntryType = typeof(NotAPlugin).FullName!
        };

        var instance = loader.TryLoad<IPlugin>(manifest, "broken.dll");

        Assert.Null(instance);
    }

    [Fact]
    public void WhenRequiredManifestTypeIsMissingThenLoadRequiredThrows() {
        var registeredPlugins = new List<IPlugin>();
        var loader = CreateLoader(registeredPlugins);
        var manifest = new PluginManifest {
            Name = "MissingType",
            EntryType = "Missing.Type"
        };

        var action = () => loader.LoadRequired<IPlugin>(manifest, "Plugins\\MissingType", "missing.dll");

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("EntryType 'Missing.Type' not found in plugin 'MissingType'.", exception.Message);
    }

    [Fact]
    public void WhenPluginLoadsThenItIsRegisteredWithTheProvidedCallback() {
        var registeredPlugins = new List<IPlugin>();
        var loader = CreateLoader(registeredPlugins);
        var manifest = new PluginManifest {
            Name = "Sample",
            EntryType = typeof(TestPlugin).FullName!
        };

        var instance = loader.LoadRequired<IPlugin>(manifest, "Plugins\\Sample", "sample.dll");

        Assert.Same(instance, Assert.Single(registeredPlugins));
    }

    private static PluginInstanceLoader CreateLoader(List<IPlugin> registeredPlugins) {
        return new PluginInstanceLoader(
            createLoadContext: _ => AssemblyLoadContext.Default,
            loadAssembly: static (_, _) => typeof(TestPlugin).Assembly,
            registerPlugin: (plugin, _) => registeredPlugins.Add(plugin),
            unloadLoadContext: static _ => { },
            diagnostics: new PluginDiagnostics(traceWriter: _ => { }, errorWriter: _ => { }));
    }

    private sealed class TestPlugin : IPlugin {
        public string Name => "Test";

        public void Initialize(IPluginContext context) {
        }

        public void Dispose() {
        }
    }

    private sealed class NotAPlugin {
    }
}
