using System.Runtime.Loader;
using Kx.Core.Plugin;
using Kx.Sdk.Plugin;

namespace Kx.Tests;

public sealed class PluginRegistryServiceTests {
    [Fact]
    public void WhenPluginIsRegisteredThenLoadOrderContainsIt() {
        var registry = new PluginRegistryService(collectGarbage: static () => { });
        var plugin = new TestPlugin("Core");
        var context = new TestAssemblyLoadContext();

        registry.Register(plugin, context);

        Assert.Equal([plugin], registry.GetLoadOrder());
    }

    [Fact]
    public void WhenMultiplePluginsAreRegisteredThenUnloadOrderIsReversed() {
        var registry = new PluginRegistryService(collectGarbage: static () => { });
        var first = new TestPlugin("Core");
        var second = new TestPlugin("Shell");
        var firstContext = new TestAssemblyLoadContext();
        var secondContext = new TestAssemblyLoadContext();

        registry.Register(first, firstContext);
        registry.Register(second, secondContext);

        Assert.Equal([second, first], registry.GetUnloadOrder());
    }

    [Fact]
    public void WhenPluginIsUnregisteredThenItIsRemovedFromLoadOrder() {
        var registry = new PluginRegistryService(collectGarbage: static () => { });
        var plugin = new TestPlugin("Core");
        var context = new TestAssemblyLoadContext();

        registry.Register(plugin, context);
        registry.Unregister(plugin);

        Assert.Empty(registry.GetLoadOrder());
    }

    [Fact]
    public void WhenPluginIsUnloadedThenPluginIsDisposedAndRegistryStateIsCleared() {
        var garbageCollections = 0;
        var registry = new PluginRegistryService(() => garbageCollections++);
        var plugin = new TestPlugin("Core");
        var context = new TestAssemblyLoadContext();

        registry.Register(plugin, context);
        registry.Unload(plugin);

        Assert.True(plugin.IsDisposed);
        Assert.False(registry.TryGetContext(plugin, out _));
        Assert.Empty(registry.GetLoadOrder());
        Assert.Equal(1, garbageCollections);
    }

    private sealed class TestPlugin(string name) : IPlugin {
        public string Name { get; } = name;
        public bool IsDisposed { get; private set; }

        public void Initialize(IPluginContext context) {
        }

        public void Dispose() {
            IsDisposed = true;
        }
    }

    private sealed class TestAssemblyLoadContext() : AssemblyLoadContext(isCollectible: true) {
    }
}
