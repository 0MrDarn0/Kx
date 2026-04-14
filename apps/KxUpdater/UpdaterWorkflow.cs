// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Event;
using Kx.Core.Pipeline;
using Kx.Core.Update;
using Kx.Sdk.Events;
using Kx.Sdk.Logging;

using KxUpdater.Configuration;

namespace KxUpdater;

internal sealed class UpdaterWorkflow {
    private readonly AppConfig _appConfig;
    private readonly IEventManager _eventManager;
    private readonly ILoggingService _logger;
    private readonly Action<string> _setStatusText;
    private readonly Action<string> _applyChangelogEntries;
    private readonly Action<float> _setProgressValue;
    private readonly Action<bool> _setProgressVisible;
    private readonly Action<bool, bool, bool> _setUpdaterInteractionState;
    private readonly Func<bool> _getUpdateRequiredState;
    private bool _eventsRegistered;

    public UpdaterWorkflow(
        AppConfig appConfig,
        IEventManager eventManager,
        ILoggingService logger,
        Action<string> setStatusText,
        Action<string> applyChangelogEntries,
        Action<float> setProgressValue,
        Action<bool> setProgressVisible,
        Action<bool, bool, bool> setUpdaterInteractionState,
        Func<bool> getUpdateRequiredState) {
        ArgumentNullException.ThrowIfNull(appConfig);
        ArgumentNullException.ThrowIfNull(eventManager);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(setStatusText);
        ArgumentNullException.ThrowIfNull(applyChangelogEntries);
        ArgumentNullException.ThrowIfNull(setProgressValue);
        ArgumentNullException.ThrowIfNull(setProgressVisible);
        ArgumentNullException.ThrowIfNull(setUpdaterInteractionState);
        ArgumentNullException.ThrowIfNull(getUpdateRequiredState);

        _appConfig = appConfig;
        _eventManager = eventManager;
        _logger = logger;
        _setStatusText = setStatusText;
        _applyChangelogEntries = applyChangelogEntries;
        _setProgressValue = setProgressValue;
        _setProgressVisible = setProgressVisible;
        _setUpdaterInteractionState = setUpdaterInteractionState;
        _getUpdateRequiredState = getUpdateRequiredState;
    }

    public void RegisterEvents() {
        if (_eventsRegistered)
            return;

        _eventsRegistered = true;
        _eventManager.Register<StatusEvent>(OnStatusChanged);
        _eventManager.Register<ChangelogEvent>(OnChangelogChanged);
        _eventManager.Register<ProgressEvent>(OnProgressChanged);
        _eventManager.Register<UpdateRequired>(OnUpdateRequired);
        _eventManager.Register<UpdatePipelineStarted>(OnUpdatePipelineStarted);
        _eventManager.Register<UpdatePipelineCompleted>(OnUpdatePipelineCompleted);
    }

    public async Task RunInitialUpdateCheckAsync() {
        try {
            if (string.IsNullOrWhiteSpace(_appConfig.Updater.Url)) {
                _setUpdaterInteractionState(false, true, true);
                return;
            }

            var runner = CreateRunner();
            _setUpdaterInteractionState(false, false, false);

            bool updateRequired = await runner.CheckForUpdatesAsync(AppContext.BaseDirectory);
            _setUpdaterInteractionState(updateRequired, true, !updateRequired);
        }
        catch (Exception ex) {
            _logger.Error("Failed to start updater pipeline.", ex);
            _setUpdaterInteractionState(false, true, true);
        }
    }

    public async Task ExecuteUpdateAsync() {
        if (string.IsNullOrWhiteSpace(_appConfig.Updater.Url)) {
            _setUpdaterInteractionState(false, true, true);
            return;
        }

        var runner = CreateRunner();
        _setUpdaterInteractionState(true, false, false);
        _setProgressVisible(true);

        try {
            await runner.RunAsync(AppContext.BaseDirectory);

            if (_getUpdateRequiredState())
                _setUpdaterInteractionState(true, true, false);
        }
        finally {
            _setProgressVisible(false);
        }
    }

    private UpdaterPipelineRunner CreateRunner() {
        return new UpdaterPipelineRunner(_eventManager, new HttpUpdateSource(), _appConfig.Updater.Url, AppContext.BaseDirectory);
    }

    private void OnStatusChanged(StatusEvent statusEvent) {
        _setStatusText(statusEvent.Text);
    }

    private void OnChangelogChanged(ChangelogEvent changelogEvent) {
        _applyChangelogEntries(changelogEvent.Text);
    }

    private void OnProgressChanged(ProgressEvent progressEvent) {
        _setProgressValue(progressEvent.Percent);
    }

    private void OnUpdateRequired(UpdateRequired _) {
        _setUpdaterInteractionState(true, true, false);
    }

    private void OnUpdatePipelineStarted(UpdatePipelineStarted _) {
        _setUpdaterInteractionState(_getUpdateRequiredState(), false, false);
    }

    private void OnUpdatePipelineCompleted(UpdatePipelineCompleted _) {
        _setProgressValue(0f);
        _setUpdaterInteractionState(false, true, true);
    }
}
