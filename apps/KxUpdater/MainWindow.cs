// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;
using Kx.Core.Configuration;
using Kx.Core.Localization;
using Kx.Sdk.Logging;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.Themes;
using Kx.Sdk.WindowHost;
using Kx.UI.Platform;
using Kx.Utility;

using KxUpdater.Configuration;

namespace KxUpdater;

public sealed class MainWindow : Window {
    private const string PrimaryActionCommandName = "kxUpdater.primaryAction";
    private const string StartGameCommandName = "kxUpdater.startGame";
    private const string OpenSettingsCommandName = "kxUpdater.openSettings";
    private const string OpenWebsiteCommandName = "kxUpdater.openWebsite";

    private static readonly UiStateKey<string> _subtitleState = new("updater.subtitle");
    private static readonly UiStateKey<string> _statusState = new("updater.status");
    private static readonly UiStateKey<string> _changelogState = new("updater.changelog");
    private static readonly UiStateKey<string[]> _newsTitlesState = new("updater.news.items");
    private static readonly UiStateKey<int> _newsSelectedIndexState = new("updater.news.selectedIndex");
    private static readonly UiStateKey<bool> _serverStatusEnabledState = new("updater.serverStatus.enabled");
    private static readonly UiStateKey<string> _serverStatusDisplayNameState = new("updater.serverStatus.displayName");
    private static readonly UiStateKey<string> _serverStatusHostState = new("updater.serverStatus.host");
    private static readonly UiStateKey<int> _serverStatusPortState = new("updater.serverStatus.port");
    private static readonly UiStateKey<int> _serverStatusCheckIntervalState = new("updater.serverStatus.checkIntervalSeconds");
    private static readonly UiStateKey<int> _serverStatusConnectTimeoutState = new("updater.serverStatus.connectTimeoutMilliseconds");
    private static readonly UiStateKey<string> _serverStatusCheckingTextState = new("updater.serverStatus.checkingText");
    private static readonly UiStateKey<string> _serverStatusOnlineTextState = new("updater.serverStatus.onlineText");
    private static readonly UiStateKey<string> _serverStatusOfflineTextState = new("updater.serverStatus.offlineText");
    private static readonly UiStateKey<string> _serverStatusTimeoutTextState = new("updater.serverStatus.timeoutText");
    private static readonly UiStateKey<float> _progressState = new("updater.progress");
    private static readonly UiStateKey<bool> _progressVisibleState = new("updater.progressVisible");
    private static readonly UiStateKey<string> _startButtonTextState = new("updater.buttons.start");
    private static readonly UiStateKey<bool> _primaryButtonEnabledState = new("updater.buttons.primaryEnabled");
    private static readonly UiStateKey<bool> _updateRequiredState = new("updater.buttons.updateRequired");
    private static readonly UiStateKey<string> _settingsButtonTextState = new("updater.buttons.settings");
    private static readonly UiStateKey<bool> _settingsButtonEnabledState = new("updater.buttons.settingsEnabled");
    private static readonly UiStateKey<string> _websiteButtonTextState = new("updater.buttons.website");

    private readonly AppConfig _appConfig;
    private readonly UpdaterLauncher _launcher;
    private readonly UpdaterWorkflow _updaterWorkflow;
    private NewsCoordinator? _newsCoordinator;
    private bool _initialUpdateCheckStarted;
    private bool _primaryActionInProgress;

    public MainWindow(IWindowHost host, ITrayService tray, ILoggingService log, IMarkupActionRegistry actionRegistry, IUiCommandRegistry commandRegistry, IUiStateStore stateStore, IControlRegistry controlRegistry, IWindowFrameRegistry windowFrameRegistry, IWindowContentRegistry windowContentRegistry)
        : base(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, windowFrameRegistry, windowContentRegistry) {
        _appConfig = ConfigLoader.Load<AppConfig>(Paths.GetConfig("app.yaml"));
        _appConfig.ServerStatus ??= new ServerStatusConfig();
        _launcher = new UpdaterLauncher(log, SetStatusText, _ctx.CloseWindow);
        _updaterWorkflow = new UpdaterWorkflow(
            _appConfig,
            _ctx.Events,
            log,
            SetStatusText,
            ApplyChangelogEntries,
            SetProgressValue,
            SetProgressVisible,
            SetUpdaterInteractionState,
            GetUpdateRequiredState);
        RegisterUpdaterCommands(commandRegistry);
    }

    protected override void OnInitialize() {
        base.OnInitialize();

        EnsureNewsSelectionBinding();

        InitializeUpdaterState();

        if (HasConfiguredControls)
            return;

        BuildUI();
    }

    protected override async void OnShown() {
        base.OnShown();

        _updaterWorkflow.RegisterEvents();

        if (_initialUpdateCheckStarted)
            return;

        _initialUpdateCheckStarted = true;
        await _updaterWorkflow.RunInitialUpdateCheckAsync();
    }

    public override void Dispose() {
        _newsCoordinator?.Dispose();
        base.Dispose();
    }

    private void InitializeUpdaterState() {
        ServerStatusConfig serverStatus = _appConfig.ServerStatus ?? new ServerStatusConfig();

        StateStore.Set(_subtitleState, LanguageService.Translate(UpdaterLanguageKeys.App.Subtitle));
        StateStore.Set(_statusState, LanguageService.Translate(UpdaterLanguageKeys.Status.Waiting));
        StateStore.Set(_changelogState, LanguageService.Translate(UpdaterLanguageKeys.Info.ChangelogLoading));
        StateStore.Set(_newsTitlesState, []);
        StateStore.Set(_newsSelectedIndexState, -1);
        StateStore.Set(_serverStatusEnabledState, serverStatus.Enabled);
        StateStore.Set(_serverStatusDisplayNameState, serverStatus.DisplayName);
        StateStore.Set(_serverStatusHostState, serverStatus.Host);
        StateStore.Set(_serverStatusPortState, serverStatus.Port);
        StateStore.Set(_serverStatusCheckIntervalState, serverStatus.CheckIntervalSeconds);
        StateStore.Set(_serverStatusConnectTimeoutState, serverStatus.ConnectTimeoutMilliseconds);
        StateStore.Set(_serverStatusCheckingTextState, LanguageService.Translate(UpdaterLanguageKeys.Info.ServerStatusChecking));
        StateStore.Set(_serverStatusOnlineTextState, LanguageService.Translate(UpdaterLanguageKeys.Info.ServerStatusOnline));
        StateStore.Set(_serverStatusOfflineTextState, LanguageService.Translate(UpdaterLanguageKeys.Info.ServerStatusOffline));
        StateStore.Set(_serverStatusTimeoutTextState, LanguageService.Translate(UpdaterLanguageKeys.Info.ServerStatusTimeout));
        StateStore.Set(_progressState, 0f);
        StateStore.Set(_progressVisibleState, false);
        StateStore.Set(_startButtonTextState, LanguageService.Translate(UpdaterLanguageKeys.Button.Start));
        StateStore.Set(_primaryButtonEnabledState, false);
        StateStore.Set(_updateRequiredState, false);
        StateStore.Set(_settingsButtonTextState, LanguageService.Translate(UpdaterLanguageKeys.Button.Settings));
        StateStore.Set(_settingsButtonEnabledState, false);
        StateStore.Set(_websiteButtonTextState, LanguageService.Translate(UpdaterLanguageKeys.Button.Website));
        _logger?.Info(LanguageService.Translate(UpdaterLanguageKeys.Info.OverlayHotkeys));
    }

    private void BuildUI() {
    }

    private void RegisterUpdaterCommands(IUiCommandRegistry commandRegistry) {
        ArgumentNullException.ThrowIfNull(commandRegistry);

        commandRegistry.Register(PrimaryActionCommandName, _ => ExecutePrimaryAction());

        commandRegistry.Register(StartGameCommandName, _ => ExecuteProcessCommand(
            _appConfig.Launcher.Start,
            UpdaterLanguageKeys.Status.ClientNotConfigured,
            UpdaterLanguageKeys.Status.ClientFileMissing,
            UpdaterLanguageKeys.Status.ClientLaunchStarted,
            UpdaterLanguageKeys.Status.ClientLaunchFailed));

        commandRegistry.Register(OpenSettingsCommandName, _ => ExecuteProcessCommand(
            _appConfig.Launcher.Settings,
            UpdaterLanguageKeys.Status.SettingsNotConfigured,
            UpdaterLanguageKeys.Status.SettingsFileMissing,
            UpdaterLanguageKeys.Status.SettingsLaunchStarted,
            UpdaterLanguageKeys.Status.SettingsLaunchFailed));

        commandRegistry.Register(OpenWebsiteCommandName, _ => ExecuteWebsiteCommand());
    }

    private async void ExecutePrimaryAction() {
        if (_primaryActionInProgress || !GetPrimaryButtonEnabledState())
            return;

        bool updateRequired = GetUpdateRequiredState();

        try {
            _primaryActionInProgress = true;

            if (updateRequired) {
                SetUpdaterInteractionState(updateRequired: true, primaryEnabled: false, settingsEnabled: false);
                await _updaterWorkflow.ExecuteUpdateAsync();
                return;
            }

            ExecuteProcessCommand(
                _appConfig.Launcher.Start,
                UpdaterLanguageKeys.Status.ClientNotConfigured,
                UpdaterLanguageKeys.Status.ClientFileMissing,
                UpdaterLanguageKeys.Status.ClientLaunchStarted,
                UpdaterLanguageKeys.Status.ClientLaunchFailed);
        }
        catch (Exception ex) {
            _logger?.Error("Failed to execute updater primary action.", ex);
            bool currentUpdateRequired = GetUpdateRequiredState();
            SetUpdaterInteractionState(currentUpdateRequired, primaryEnabled: true, settingsEnabled: !currentUpdateRequired);
        }
        finally {
            _primaryActionInProgress = false;

            if (!updateRequired)
                SetUpdaterInteractionState(updateRequired: false, primaryEnabled: true, settingsEnabled: true);
        }
    }

    private void ExecuteProcessCommand(ProcessLaunchConfig config, LanguageKey notConfiguredStatusKey, LanguageKey fileMissingStatusKey, LanguageKey startedStatusKey, LanguageKey failedStatusKey) {
        _launcher.ExecuteProcessCommand(config, notConfiguredStatusKey, fileMissingStatusKey, startedStatusKey, failedStatusKey);
    }

    private void ExecuteWebsiteCommand() {
        _launcher.ExecuteWebsiteCommand(_appConfig.Launcher.Website.Url);
    }

    private void EnsureNewsSelectionBinding() {
        GetNewsCoordinator().EnsureNewsSelectionBinding();
    }

    private void ApplyChangelogEntries(string changelogText) {
        GetNewsCoordinator().ApplyChangelogEntries(changelogText);
    }

    private void SetStatusText(string statusText) {
        StateStore.Set(_statusState, statusText);
    }

    private void SetChangelogText(string changelogText) {
        StateStore.Set(_changelogState, changelogText);
    }

    private void SetProgressValue(float progressValue) {
        StateStore.Set(_progressState, progressValue);
    }

    private void SetProgressVisible(bool progressVisible) {
        StateStore.Set(_progressVisibleState, progressVisible);
    }

    private bool GetUpdateRequiredState() {
        return StateStore.TryGet(_updateRequiredState, out bool updateRequired) && updateRequired;
    }

    private bool GetPrimaryButtonEnabledState() {
        return StateStore.TryGet(_primaryButtonEnabledState, out bool isEnabled) && isEnabled;
    }

    private void SetUpdaterInteractionState(bool updateRequired, bool primaryEnabled, bool settingsEnabled) {
        StateStore.Set(_updateRequiredState, updateRequired);
        StateStore.Set(_primaryButtonEnabledState, primaryEnabled);
        StateStore.Set(_settingsButtonEnabledState, settingsEnabled);
        StateStore.Set(_startButtonTextState, LanguageService.Translate(updateRequired ? UpdaterLanguageKeys.Button.Update : UpdaterLanguageKeys.Button.Start));
    }

    private NewsCoordinator GetNewsCoordinator() {
        return _newsCoordinator ??= new NewsCoordinator(
            subscribeToSelectedIndex: listener => StateStore.Subscribe(_newsSelectedIndexState, listener),
            setNewsTitles: newsTitles => StateStore.Set(_newsTitlesState, newsTitles),
            setSelectedIndex: selectedIndex => StateStore.Set(_newsSelectedIndexState, selectedIndex),
            setChangelogText: SetChangelogText);
    }
}
