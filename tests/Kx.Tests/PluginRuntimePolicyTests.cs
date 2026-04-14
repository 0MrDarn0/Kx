using Kx.Core.DI;
using Kx.Core.Plugin;
using Kx.Sdk.DI;
using Kx.Sdk.Logging;
using Kx.Sdk.Plugin;

namespace Kx.Tests;

public sealed class PluginRuntimePolicyTests {
    [Fact]
    public void WhenServicePluginConfigurationFailsThenPluginIsUnloaded() {
        var plugin = new ThrowingServicePlugin("Broken");
        IPlugin? unloadedPlugin = null;
        var policy = new PluginRuntimePolicy(
            loadPlugins: () => [plugin],
            unloadPlugin: plugin => unloadedPlugin = plugin);
        var services = new MsDiContainer();

        policy.ConfigureServices(services);

        Assert.Same(plugin, unloadedPlugin);
    }

    [Fact]
    public void WhenPluginsInitializeThenLoadOrderIsPreserved() {
        var services = CreateBuiltServices();
        var initializedPlugins = new List<string>();
        var first = new RecordingPlugin("First", initializedPlugins);
        var second = new RecordingPlugin("Second", initializedPlugins);
        var policy = new PluginRuntimePolicy(getLoadOrder: () => [first, second]);

        policy.InitializePlugins(services, new TestLoggingService());

        Assert.Equal(["First", "Second"], initializedPlugins);
    }

    [Fact]
    public void WhenPluginInitializationFailsThenPluginIsUnloaded() {
        var services = CreateBuiltServices();
        var plugin = new ThrowingPlugin("Broken");
        IPlugin? unloadedPlugin = null;
        var policy = new PluginRuntimePolicy(
            getLoadOrder: () => [plugin],
            unloadPlugin: candidate => unloadedPlugin = candidate);

        policy.InitializePlugins(services, new TestLoggingService());

        Assert.Same(plugin, unloadedPlugin);
    }

    [Fact]
    public void WhenPluginsShutdownThenUnloadOrderIsPreserved() {
        var unloadedPlugins = new List<string>();
        var first = new RecordingPlugin("First", []);
        var second = new RecordingPlugin("Second", []);
        var policy = new PluginRuntimePolicy(
            getUnloadOrder: () => [second, first],
            unloadPlugin: plugin => unloadedPlugins.Add(plugin.Name));

        policy.ShutdownPlugins(new TestLoggingService());

        Assert.Equal(["Second", "First"], unloadedPlugins);
    }

    private static MsDiContainer CreateBuiltServices() {
        var services = new MsDiContainer();
        services.Register<ILoggerFactory>(new TestLoggerFactory());
        services.Build();
        return services;
    }

    private sealed class RecordingPlugin(string name, List<string> initializedPlugins) : IPlugin {
        public string Name { get; } = name;

        public void Initialize(IPluginContext context) {
            initializedPlugins.Add(Name);
        }

        public void Dispose() {
        }
    }

    private sealed class ThrowingPlugin(string name) : IPlugin {
        public string Name { get; } = name;

        public void Initialize(IPluginContext context) {
            throw new InvalidOperationException("Plugin initialization failed.");
        }

        public void Dispose() {
        }
    }

    private sealed class ThrowingServicePlugin(string name) : IServicePlugin {
        public string Name { get; } = name;

        public void ConfigureServices(IServiceRegistry services) {
            throw new InvalidOperationException("Service registration failed.");
        }

        public void Initialize(IPluginContext context) {
        }

        public void Dispose() {
        }
    }

    private sealed class TestLoggerFactory : ILoggerFactory {
        public ILoggingService CreateLogger(string category) => new TestLoggingService();
    }

    private sealed class TestLoggingService : ILoggingService {
        public void Log(LogLevel level, string message, Exception? ex = null) {
        }

        public void Trace(string message) {
        }

        public void Debug(string message) {
        }

        public void Info(string message) {
        }

        public void Warning(string message) {
        }

        public void Error(string message, Exception? ex = null) {
        }

        public void Critical(string message, Exception? ex = null) {
        }
    }
}
