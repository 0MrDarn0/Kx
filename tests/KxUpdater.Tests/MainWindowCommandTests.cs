using Kx.App;
using Kx.Sdk.Logging;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.VisualTree;
using Kx.Tests.TestInfrastructure;
using Kx.UI.Commands;
using Kx.UI.Platform;
using Kx.UI.State;
using Kx.Utility;

using KxUpdater;

namespace KxUpdater.Tests;

public sealed class MainWindowCommandTests {
    private static readonly UiStateKey<string> _statusState = new("updater.status");
    private static readonly UiStateKey<string> _primaryButtonTextState = new("updater.buttons.start");
    private static readonly UiStateKey<bool> _primaryButtonEnabledState = new("updater.buttons.primaryEnabled");
    private static readonly UiStateKey<bool> _progressVisibleState = new("updater.progressVisible");
    private static readonly UiStateKey<bool> _settingsButtonEnabledState = new("updater.buttons.settingsEnabled");

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
    public void WhenPrimaryActionRunsWhileDisabledThenItIsIgnored() {
        using var window = CreateMainWindow(out _, out var commandRegistry, out var stateStore);

        bool executed = commandRegistry.TryExecute(new TestUiCommandContext("kxUpdater.primaryAction"));

        Assert.True(executed);
        Assert.True(stateStore.TryGet(_statusState, out var status));
        Assert.Equal("Checking version...", status);
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
    public void WhenMainWindowInitializesThenOverlayHotkeysAreLogged() {
        var logger = new TestLoggingService();
        using var window = CreateMainWindow(logger, out _, out _, out _);

        Assert.Contains(logger.InfoMessages, message => message.Contains("Ctrl+Shift+D", StringComparison.Ordinal));
    }

    [Fact]
    public void WhenMainWindowInitializesThenPrimaryActionStartsDisabledWithStartText() {
        using var window = CreateMainWindow(out _, out _, out var stateStore);

        Assert.True(stateStore.TryGet(_primaryButtonEnabledState, out bool isEnabled));
        Assert.False(isEnabled);
        Assert.True(stateStore.TryGet(_settingsButtonEnabledState, out bool settingsEnabled));
        Assert.False(settingsEnabled);
        Assert.True(stateStore.TryGet(_progressVisibleState, out bool progressVisible));
        Assert.False(progressVisible);
        Assert.True(stateStore.TryGet(_primaryButtonTextState, out var buttonText));
        Assert.Equal("Start", buttonText);
    }

    [Fact]
    public void WhenPrimaryActionRunsWithoutPendingUpdateThenGameStartBehaviorIsUsed() {
        using var window = CreateMainWindow(out var host, out var commandRegistry, out var stateStore);
        stateStore.Set(_primaryButtonEnabledState, true);

        bool executed = commandRegistry.TryExecute(new TestUiCommandContext("kxUpdater.primaryAction"));

        Assert.True(executed);
        Assert.True(stateStore.TryGet(_statusState, out var status));
        Assert.Equal("Game client file not found: engine.exe", status);
        Assert.Equal(0, host.CloseWindowCallCount);
    }

    private static MainWindow CreateMainWindow(out TestWindowHost host, out UiCommandRegistry commandRegistry, out UiStateStore stateStore) {
        return CreateMainWindow(new TestLoggingService(), out host, out commandRegistry, out stateStore);
    }

    private static MainWindow CreateMainWindow(TestLoggingService logger, out TestWindowHost host, out UiCommandRegistry commandRegistry, out UiStateStore stateStore) {
        EnsureUpdaterAssetsForTests();

        host = new TestWindowHost();
        var uiComposition = new RuntimeUiComposition();
        commandRegistry = uiComposition.CommandRegistry;
        stateStore = uiComposition.StateStore;

        return new MainWindow(
            host,
            new TestTrayService(),
            logger,
            uiComposition.ActionRegistry,
            commandRegistry,
            stateStore,
            uiComposition.ControlRegistry,
            uiComposition.ThemeRegistry,
            uiComposition.WindowRegistry);
    }

    private static void EnsureUpdaterAssetsForTests() {
        Directory.CreateDirectory(Paths.CfgFolder);
        Directory.CreateDirectory(Paths.LangFolder);

        File.WriteAllText(Paths.GetConfig("app.yaml"),
            """
            updater:
              url: https://update.idb-lab.de/
            launcher:
              start:
                fileName: "engine.exe"
                arguments: "/load /config debug"
                workingDirectory: ""
                resolveFromAppDirectory: true
                closeUpdaterOnSuccess: true
              settings:
                fileName: "engine.exe"
                arguments: "/setup"
                workingDirectory: ""
                resolveFromAppDirectory: true
                closeUpdaterOnSuccess: false
              website:
                url: http://www.idb-lab.de/
            ui:
              language: en
            window:
              icon: Icons:app.ico
            """);

        File.WriteAllText(Paths.GetLang("en"),
            """
            app:
              subtitle: "Updater"
            status:
              waiting: "Checking version..."
              client_not_configured: "Game client not configured."
              client_file_missing: "Game client file not found: {0}"
              settings_not_configured: "Settings executable not configured."
              settings_file_missing: "Settings executable not found: {0}"
              client_launch_started: "Game client started."
              client_launch_failed: "Failed to start game client."
              settings_launch_started: "Settings started."
              settings_launch_failed: "Failed to start settings executable."
            button:
              start: "Start"
              update: "Update"
              settings: "Settings"
              website: "Website"
            info:
              changelog_loading: "Loading changelog..."
              overlay_hotkeys: "Overlay hotkeys: Ctrl+Shift+D"
            """);
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
        public List<string> InfoMessages { get; } = [];

        public void Log(LogLevel level, string message, Exception? ex = null) {
        }

        public void Trace(string message) {
        }

        public void Debug(string message) {
        }

        public void Info(string message) {
            InfoMessages.Add(message);
        }

        public void Warning(string message) {
        }

        public void Error(string message, Exception? ex = null) {
        }

        public void Critical(string message, Exception? ex = null) {
        }
    }
}
