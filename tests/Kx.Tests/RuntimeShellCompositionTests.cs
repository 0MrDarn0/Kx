using Kx.App;
using Kx.Core.DI;
using Kx.Core.Lifecycle;
using Kx.Core.Plugin;
using Kx.Sdk.Logging;
using Kx.Sdk.WindowHost;
using Kx.Tests.TestInfrastructure;
using Kx.UI.Platform;

namespace Kx.Tests;

public sealed class RuntimeShellCompositionTests {
    [Fact]
    public void WhenDefaultsAreRegisteredWithCompositionThenContainerUsesTheSameTrayIconInstance() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var uiComposition = new RuntimeUiComposition();
        var loggingComposition = new RuntimeLoggingComposition();
        var shellComposition = new RuntimeShellComposition();

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager, uiComposition, loggingComposition, shellComposition);
        services.Build();

        Assert.Same(shellComposition.TrayIcon, services.Get<TrayIcon>());
    }

    [Fact]
    public void WhenDefaultsAreRegisteredWithCompositionThenLifecycleManagersResolveFromShellComposition() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var uiComposition = new RuntimeUiComposition();
        var loggingComposition = new RuntimeLoggingComposition();
        var shellComposition = new RuntimeShellComposition();

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager, uiComposition, loggingComposition, shellComposition);
        services.Build();

        Assert.IsType<StartupManager>(services.Get<StartupManager>());
        Assert.IsType<ShutdownManager>(services.Get<ShutdownManager>());
        Assert.IsAssignableFrom<ITrayService>(services.Get<ITrayService>());
    }
}
