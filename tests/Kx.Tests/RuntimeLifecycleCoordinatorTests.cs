using Kx.App;
using Kx.Core.DI;
using Kx.Core.Plugin;
using Kx.Sdk.Lifecycle;
using Kx.Tests.TestInfrastructure;

namespace Kx.Tests;

public sealed class RuntimeLifecycleCoordinatorTests {
    [Fact]
    public async Task WhenStartAsyncRunsThenStartupAwareServicesAreInvoked() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var startupProbe = new StartupProbe();
        var coordinator = new RuntimeLifecycleCoordinator(services, pluginManager);

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager);
        services.Register<IStartupAware>(startupProbe);

        await coordinator.StartAsync();

        Assert.True(startupProbe.WasStarted);
    }

    [Fact]
    public async Task WhenShutdownAsyncRunsThenShutdownAwareServicesAreInvoked() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var shutdownProbe = new ShutdownProbe();
        var coordinator = new RuntimeLifecycleCoordinator(services, pluginManager);

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager);
        services.Register<IShutdownAware>(shutdownProbe);

        await coordinator.StartAsync();
        await coordinator.ShutdownAsync();

        Assert.True(shutdownProbe.WasShutdown);
    }

    private sealed class StartupProbe : IStartupAware {
        public bool WasStarted { get; private set; }

        public ValueTask StartupAsync() {
            WasStarted = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ShutdownProbe : IShutdownAware {
        public bool WasShutdown { get; private set; }

        public ValueTask ShutdownAsync() {
            WasShutdown = true;
            return ValueTask.CompletedTask;
        }
    }
}
