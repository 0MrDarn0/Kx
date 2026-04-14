using System.Runtime.Loader;
using Kx.Core.DI;
using Kx.Core.Plugin;
using Kx.Sdk.Logging;
using Kx.Sdk.Plugin;

namespace Kx.Tests;

public sealed class PluginRuntimeCompositionTests {
    [Fact]
    public void WhenCompositionLoadsPluginsThenPolicyInitializesTheSameRegisteredInstances() {
        var diagnostics = new PluginDiagnostics(traceWriter: _ => { }, errorWriter: _ => { });
        var registry = new PluginRegistryService(collectGarbage: static () => { });
        var instanceLoader = new PluginInstanceLoader(
            createLoadContext: _ => AssemblyLoadContext.Default,
            loadAssembly: static (_, _) => typeof(ObservablePlugin).Assembly,
            registerPlugin: registry.Register,
            unloadLoadContext: static _ => { },
            diagnostics: diagnostics);
        var composition = new PluginRuntimeComposition(
            pluginRoot: AppContext.BaseDirectory,
            discoverPlugins: (_, _) => new Dictionary<string, PluginCatalogEntry>(StringComparer.OrdinalIgnoreCase) {
                ["Sample"] = new PluginCatalogEntry(
                    "Sample",
                    "Plugins\\Sample",
                    new PluginManifest {
                        Name = "Sample",
                        ApiVersion = global::Kx.Core.HostInfo.ApiVersion,
                        EntryType = typeof(ObservablePlugin).FullName!
                    },
                    "Sample.dll")
            },
            diagnostics: diagnostics,
            registry: registry,
            instanceLoader: instanceLoader);
        var services = CreateBuiltServices();

        var plugin = Assert.IsType<ObservablePlugin>(Assert.Single(composition.Loader.LoadAll<IPlugin>()));

        composition.Policy.InitializePlugins(services, new TestLoggingService());

        Assert.True(plugin.WasInitialized);
    }

    [Fact]
    public void WhenCompositionIsCreatedThenLoaderAndPolicyShareTheSameRegistry() {
        var registry = new PluginRegistryService(collectGarbage: static () => { });
        var composition = new PluginRuntimeComposition(registry: registry, diagnostics: new PluginDiagnostics(traceWriter: _ => { }, errorWriter: _ => { }));

        Assert.Same(registry, composition.Registry);
    }

    private static MsDiContainer CreateBuiltServices() {
        var services = new MsDiContainer();
        services.Register<ILoggerFactory>(new TestLoggerFactory());
        services.Build();
        return services;
    }

    private sealed class ObservablePlugin : IPlugin {
        public string Name => "Sample";
        public bool WasInitialized { get; private set; }

        public void Initialize(IPluginContext context) {
            WasInitialized = true;
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
