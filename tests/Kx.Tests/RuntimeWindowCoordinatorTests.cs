using Kx.App;
using Kx.Core.DI;
using Kx.Sdk.WindowHost;
using Kx.Tests.TestInfrastructure;

namespace Kx.Tests;

public sealed class RuntimeWindowCoordinatorTests {
    [Fact]
    public void WhenShowRunsThenCreatedWindowMatchesRequestedType() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var coordinator = new RuntimeWindowCoordinator(services, windowHost);

        services.Register<IWindowHost>(windowHost);
        services.Build();

        using var window = coordinator.Show(typeof(TestWindow), static () => Task.CompletedTask);

        Assert.IsType<TestWindow>(window);
    }

    [Fact]
    public void WhenShowRunsThenHostWindowIsShown() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var coordinator = new RuntimeWindowCoordinator(services, windowHost);

        services.Register<IWindowHost>(windowHost);
        services.Build();

        using var window = coordinator.Show(typeof(TestWindow), static () => Task.CompletedTask);

        Assert.Equal(1, windowHost.ShowWindowCallCount);
    }

    [Fact]
    public async Task WhenHostClosesAfterShowThenShutdownCallbackRuns() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var coordinator = new RuntimeWindowCoordinator(services, windowHost);
        var shutdownCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        services.Register<IWindowHost>(windowHost);
        services.Build();

        using var window = coordinator.Show(typeof(TestWindow), () => {
            shutdownCompletion.SetResult();
            return Task.CompletedTask;
        });

        windowHost.RaiseClosed();

        await shutdownCompletion.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(shutdownCompletion.Task.IsCompletedSuccessfully);
    }

    private sealed class TestWindow : Window {
        public TestWindow(IWindowHost host)
            : base(host, null, null) {
        }

        protected override void InitializeFrame() {
        }

        protected override void InitializeRenderer() {
        }

        protected override void InitializeInteraction() {
        }

        protected override void RegisterWindowEvents() {
        }

        protected override void OnInitialize() {
        }

        protected override void InitializeConfiguredControls() {
        }

        public override void Dispose() {
        }
    }
}
