using Kx.App;
using Kx.Core.DI;
using Kx.Core.Logging;
using Kx.Core.Plugin;
using Kx.Sdk.Logging;
using Kx.Sdk.WindowHost;
using Kx.Tests.TestInfrastructure;

namespace Kx.Tests;

public sealed class RuntimeLoggingCompositionTests {
    [Fact]
    public void WhenDefaultsAreRegisteredWithCompositionThenContainerUsesTheSameLogSinkInstances() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var uiComposition = new RuntimeUiComposition();
        var loggingComposition = new RuntimeLoggingComposition();

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager, uiComposition, loggingComposition);
        services.Build();

        Assert.Contains(loggingComposition.DebugLogSink, services.GetAll<ILogSink>());
        Assert.Contains(loggingComposition.FileLogSink, services.GetAll<ILogSink>());
    }

    [Fact]
    public void WhenDefaultsAreRegisteredWithCompositionThenSystemLoggerResolvesFromLoggingComposition() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var uiComposition = new RuntimeUiComposition();
        var loggingComposition = new RuntimeLoggingComposition();

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager, uiComposition, loggingComposition);
        services.Build();

        Assert.IsType<LoggerFactory>(services.Get<ILoggerFactory>());
        Assert.IsAssignableFrom<ILoggingService>(services.Get<ILoggingService>());
    }
}
