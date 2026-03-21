using Kx.App;
using Kx.Core.DI;
using Kx.Core.Lifecycle;
using Kx.Core.Plugin;
using Kx.Sdk.Events;
using Kx.Sdk.Lifecycle;
using Kx.Sdk.Logging;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.WindowHost;
using Kx.Tests.TestInfrastructure;
using Kx.UI.Markup;

namespace Kx.Tests;

public sealed class RuntimeServiceConfigurationTests {
    [Fact]
    public void WhenDefaultsAreRegisteredThenWindowHostResolvesFromContainer() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager);
        services.Build();

        Assert.Same(windowHost, services.Get<IWindowHost>());
    }

    [Fact]
    public void WhenDefaultsAreRegisteredThenStartupManagerResolvesFromContainer() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager);
        services.Build();

        Assert.IsType<StartupManager>(services.Get<StartupManager>());
    }

    [Fact]
    public void WhenDefaultsAreRegisteredThenPluginManagerParticipatesInShutdownLifecycle() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager);
        services.Build();

        Assert.Contains(pluginManager, services.GetAll<IShutdownAware>());
    }

    [Fact]
    public void WhenApplicationServicesRegisterAfterDefaultsThenTheyOverrideDefaultContracts() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var customWindowRegistry = new WindowRegistry();

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager);
        services.Register<IWindowRegistry>(customWindowRegistry);
        services.Build();

        Assert.Same(customWindowRegistry, services.Get<IWindowRegistry>());
    }

    [Fact]
    public void WhenDefaultsAreRegisteredThenSystemLoggingServiceResolvesFromContainer() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager);
        services.Build();

        Assert.IsAssignableFrom<ILoggingService>(services.Get<ILoggingService>());
    }
}
