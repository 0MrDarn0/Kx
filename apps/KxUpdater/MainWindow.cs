// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.ComponentModel;
using System.Diagnostics;

using Kx.App;
using Kx.Core.Configuration;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Core.Pipeline;
using Kx.Core.Update;
using Kx.Sdk.Logging;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.Themes;
using Kx.Sdk.WindowHost;
using Kx.UI.Elements.Panel;
using Kx.UI.Layout;
using Kx.UI.Platform;
using Kx.Utility;

using KxUpdater.Configuration;

namespace KxUpdater;

public sealed class MainWindow : Window {
    private const string StartGameCommandName = "kxUpdater.startGame";
    private const string OpenSettingsCommandName = "kxUpdater.openSettings";
    private const string OpenWebsiteCommandName = "kxUpdater.openWebsite";

    private static readonly UiStateKey<string> _subtitleState = new("updater.subtitle");
    private static readonly UiStateKey<string> _statusState = new("updater.status");
    private static readonly UiStateKey<string> _changelogState = new("updater.changelog");
    private static readonly UiStateKey<float> _progressState = new("updater.progress");
    private static readonly UiStateKey<string> _startButtonTextState = new("updater.buttons.start");
    private static readonly UiStateKey<string> _settingsButtonTextState = new("updater.buttons.settings");
    private static readonly UiStateKey<string> _websiteButtonTextState = new("updater.buttons.website");

    private readonly AppConfig _appConfig;
    private bool _updaterEventsRegistered;
    private bool _pipelineStarted;

    public MainWindow(IWindowHost host, ITrayService tray, ILoggingService log, IMarkupActionRegistry actionRegistry, IUiCommandRegistry commandRegistry, IUiStateStore stateStore, IControlRegistry controlRegistry, IThemeRegistry themeRegistry, IWindowRegistry windowRegistry)
        : base(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, themeRegistry, windowRegistry) {
        _appConfig = ConfigLoader.Load<AppConfig>(Paths.GetConfig("app.yaml"));
        RegisterUpdaterCommands(commandRegistry);
    }

    protected override void OnInitialize() {
        base.OnInitialize();

        InitializeUpdaterState();

        if (HasConfiguredControls)
            return;

        BuildUI();
    }

    protected override async void OnShown() {
        base.OnShown();

        RegisterUpdaterEvents();
        if (_pipelineStarted)
            return;

        _pipelineStarted = true;

        try {
            if (string.IsNullOrWhiteSpace(_appConfig.Updater.Url))
                return;

            var runner = new UpdaterPipelineRunner(_ctx.Events, new HttpUpdateSource(), _appConfig.Updater.Url, AppContext.BaseDirectory);
            await runner.RunAsync(AppContext.BaseDirectory);
        }
        catch (Exception ex) {
            _logger?.Error("Failed to start updater pipeline.", ex);
        }
    }

    private void InitializeUpdaterState() {
        StateStore.Set(_subtitleState, LanguageService.Translate("app.subtitle"));
        StateStore.Set(_statusState, LanguageService.Translate("status.waiting"));
        StateStore.Set(_changelogState, LanguageService.Translate("info.changelog_loading"));
        StateStore.Set(_progressState, 0f);
        StateStore.Set(_startButtonTextState, LanguageService.Translate("button.start"));
        StateStore.Set(_settingsButtonTextState, LanguageService.Translate("button.settings"));
        StateStore.Set(_websiteButtonTextState, LanguageService.Translate("button.website"));
    }

    private void RegisterUpdaterEvents() {
        if (_updaterEventsRegistered)
            return;

        _updaterEventsRegistered = true;
        _ctx.Events.Register<StatusEvent>(updateStatus => StateStore.Set(_statusState, updateStatus.Text));
        _ctx.Events.Register<ChangelogEvent>(changelog => StateStore.Set(_changelogState, changelog.Text));
        _ctx.Events.Register<ProgressEvent>(progress => StateStore.Set(_progressState, progress.Percent));
        _ctx.Events.Register<UpdatePipelineCompleted>(_ => StateStore.Set(_progressState, 0f));
    }

    private void RegisterUpdaterCommands(IUiCommandRegistry commandRegistry) {
        ArgumentNullException.ThrowIfNull(commandRegistry);

        commandRegistry.Register(StartGameCommandName, _ => ExecuteProcessCommand(
            _appConfig.Launcher.Start,
            "status.client_not_configured",
            "status.client_file_missing",
            "status.client_launch_started",
            "status.client_launch_failed"));

        commandRegistry.Register(OpenSettingsCommandName, _ => ExecuteProcessCommand(
            _appConfig.Launcher.Settings,
            "status.settings_not_configured",
            "status.settings_file_missing",
            "status.settings_launch_started",
            "status.settings_launch_failed"));

        commandRegistry.Register(OpenWebsiteCommandName, _ => ExecuteWebsiteCommand());
    }

    private void ExecuteProcessCommand(ProcessLaunchConfig config, string notConfiguredStatusKey, string fileMissingStatusKey, string startedStatusKey, string failedStatusKey) {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.FileName)) {
            StateStore.Set(_statusState, LanguageService.Translate(notConfiguredStatusKey));
            return;
        }

        try {
            var startInfo = CreateProcessStartInfo(config);
            if (RequiresExistingFile(config, startInfo.FileName) && !File.Exists(startInfo.FileName)) {
                StateStore.Set(_statusState, LanguageService.Translate(fileMissingStatusKey, Path.GetFileName(startInfo.FileName)));
                return;
            }

            if (Process.Start(startInfo) is null) {
                StateStore.Set(_statusState, LanguageService.Translate(failedStatusKey, "Process returned no handle."));
                return;
            }

            StateStore.Set(_statusState, LanguageService.Translate(startedStatusKey));

            if (config.CloseUpdaterOnSuccess)
                _ctx.CloseWindow();
        }
        catch (InvalidOperationException ex) {
            _logger?.Error($"Failed to execute updater command '{config.FileName}'.", ex);
            StateStore.Set(_statusState, LanguageService.Translate(failedStatusKey, ex.Message));
        }
        catch (Win32Exception ex) {
            _logger?.Error($"Failed to execute updater command '{config.FileName}'.", ex);
            StateStore.Set(_statusState, LanguageService.Translate(failedStatusKey, ex.Message));
        }
    }

    private void ExecuteWebsiteCommand() {
        var url = _appConfig.Launcher.Website.Url?.Trim();
        if (string.IsNullOrWhiteSpace(url)) {
            StateStore.Set(_statusState, LanguageService.Translate("status.website_not_configured"));
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri)) {
            StateStore.Set(_statusState, LanguageService.Translate("status.website_open_failed", "Invalid URL."));
            return;
        }

        try {
            if (Process.Start(new ProcessStartInfo(targetUri.AbsoluteUri) { UseShellExecute = true }) is null) {
                StateStore.Set(_statusState, LanguageService.Translate("status.website_open_failed", "Process returned no handle."));
                return;
            }

            StateStore.Set(_statusState, LanguageService.Translate("status.website_opening"));
        }
        catch (InvalidOperationException ex) {
            _logger?.Error($"Failed to open website '{targetUri.AbsoluteUri}'.", ex);
            StateStore.Set(_statusState, LanguageService.Translate("status.website_open_failed", ex.Message));
        }
        catch (Win32Exception ex) {
            _logger?.Error($"Failed to open website '{targetUri.AbsoluteUri}'.", ex);
            StateStore.Set(_statusState, LanguageService.Translate("status.website_open_failed", ex.Message));
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(ProcessLaunchConfig config) {
        string fileName = ResolveConfiguredPath(config.FileName, config.ResolveFromAppDirectory);
        string workingDirectory = string.IsNullOrWhiteSpace(config.WorkingDirectory)
            ? ResolveDefaultWorkingDirectory(fileName)
            : ResolveConfiguredPath(config.WorkingDirectory, resolveFromAppDirectory: true);

        return new ProcessStartInfo {
            FileName = fileName,
            Arguments = config.Arguments ?? string.Empty,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };
    }

    private static bool RequiresExistingFile(ProcessLaunchConfig config, string fileName) {
        return config.ResolveFromAppDirectory || Path.IsPathRooted(fileName);
    }

    private static string ResolveConfiguredPath(string path, bool resolveFromAppDirectory) {
        string expandedPath = Environment.ExpandEnvironmentVariables(path.Trim());
        if (Path.IsPathRooted(expandedPath))
            return expandedPath;

        if (!resolveFromAppDirectory)
            return expandedPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedPath));
    }

    private static string ResolveDefaultWorkingDirectory(string fileName) {
        if (!Path.IsPathRooted(fileName))
            return AppContext.BaseDirectory;

        return Path.GetDirectoryName(fileName) ?? AppContext.BaseDirectory;
    }

    private void BuildUI() {
        _logger?.Info($"{typeof(MainWindow).FullName} BuildUI()");
        var grid = new Grid(_ctx, id: "grid");

        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(150) });
        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });

        grid.Rows.Add(new RowDefinition { Height = GridLength.Pixel(50) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

        var header = new Kx.UI.Elements.Label(_ctx, id: "header", text: "HEADER", size: 10) {
            GridRow = 0,
            GridColumn = 0,
            GridColumnSpan = 2
        };

        var sidebar = new Kx.UI.Elements.Label(_ctx, id: "sidebar", text: "SIDEBAR", size: 10) {
            GridRow = 1,
            GridColumn = 0
        };

        var content = new Kx.UI.Elements.Label(_ctx, id: "content", text: "CONTENT", size: 10) {
            GridRow = 1,
            GridColumn = 1
        };

        grid.AddChild(header);
        grid.AddChild(sidebar);
        grid.AddChild(content);

        var btn_exit = new Kx.UI.Elements.Button(_ctx, id: "btn_exit", text: "X") {
            GridRow = 1,
            GridColumn = 1,
            Padding = new Thickness(100),
            Margin = new Thickness(100)
        };
        btn_exit.Click += () => {
            _logger?.Info($"{btn_exit.Id} Clicked!");
        };

        grid.AddChild(btn_exit);
        _ctx.UIElementManager.Add(grid);
    }
}
