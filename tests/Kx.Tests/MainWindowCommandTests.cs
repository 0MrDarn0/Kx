using Kx.App;
using Kx.Sdk.Logging;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.VisualTree;
using Kx.UI.Commands;
using Kx.UI.Platform;
using Kx.UI.State;
using Kx.Tests.TestInfrastructure;

using KxUpdater;

namespace Kx.Tests;

public sealed class MainWindowCommandTests {
    private static readonly UiStateKey<string> _statusState = new("updater.status");

    [Fact]
    public void WhenStartGameCommandRunsWithMissingClientThenStatusExplainsIt() {
        using var window = CreateMainWindow(out var host, out var commandRegistry, out var stateStore);

        bool executed = commandRegistry.TryExecute(new TestUiCommandContext("kxUpdater.startGame"));

        Assert.True(executed);
        Assert.True(stateStore.TryGet(_statusState, out var status));
        Assert.Equal("Game client file not found: engine.exe", status);
        Assert.Equal(0, host.CloseWindowCallCount);
    }

    [Fact]
    public void WhenOpenSettingsCommandRunsWithMissingExecutableThenStatusExplainsIt() {
        using var window = CreateMainWindow(out var host, out var commandRegistry, out var stateStore);

        bool executed = commandRegistry.TryExecute(new TestUiCommandContext("kxUpdater.openSettings"));

        Assert.True(executed);
        Assert.True(stateStore.TryGet(_statusState, out var status));
        Assert.Equal("Settings executable not found: engine.exe", status);
        Assert.Equal(0, host.CloseWindowCallCount);
    }

    [Fact]
    public void WhenOpenWebsiteCommandRunsWithoutConfiguredUrlThenStatusExplainsIt() {
        using var window = CreateMainWindow(out _, out var commandRegistry, out var stateStore);

        bool executed = commandRegistry.TryExecute(new TestUiCommandContext("kxUpdater.openWebsite"));

        Assert.True(executed);
        Assert.True(stateStore.TryGet(_statusState, out var status));
        Assert.Equal("Website link is not configured yet.", status);
    }

    private static MainWindow CreateMainWindow(out TestWindowHost host, out UiCommandRegistry commandRegistry, out UiStateStore stateStore) {
        host = new TestWindowHost();
        var uiComposition = new RuntimeUiComposition();
        commandRegistry = uiComposition.CommandRegistry;
        stateStore = uiComposition.StateStore;

        return new MainWindow(
            host,
            new TestTrayService(),
            new TestLoggingService(),
            uiComposition.ActionRegistry,
            commandRegistry,
            stateStore,
            uiComposition.ControlRegistry,
            uiComposition.ThemeRegistry,
            uiComposition.WindowRegistry);
    }

    private sealed class TestUiCommandContext(string commandName) : IUiCommandContext {
        private static readonly TestVisualContext _visualContext = new();
        private static readonly UIElement _source = new Kx.UI.Elements.Label(_visualContext, "source", string.Empty, 12f);

        public IVisualContext VisualContext => _visualContext;
        public UIElement Source => _source;
        public string CommandName { get; } = commandName;
        public string? Argument => null;
        public UiCommandPayload Payload { get; } = new(null);
    }

    private sealed class TestTrayService : ITrayService {
        public event EventHandler? Clicked;
        public event EventHandler? DoubleClicked;

        public void Show() {
        }

        public void Hide() {
        }

        public void SetStatus(string key) {
        }

        public void ShowBalloon(string title, string text, int timeout = 2000) {
        }

        public void Configure(Action<TrayIcon> configure) {
        }

        public void Dispose() {
        }
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
