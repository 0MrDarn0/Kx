// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Configuration;
using Kx.Core.Event;
using Kx.Sdk.Events;
using Kx.Sdk.Logging;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.Themes;
using Kx.Sdk.UI.VisualTree;
using Kx.Sdk.WindowHost;
using Kx.UI.Markup;
using Kx.UI.Platform;
using Kx.UI.Rendering;
using Kx.UI.Themes;
using Kx.Utility;

namespace Kx.App;

public abstract class Window : IDisposable {
    protected IWindowHost _host;
    protected WindowContext _ctx;
    protected WindowInteraction? _interaction;
    protected ITrayService? _tray;
    protected ILoggingService? _logger;
    protected bool HasConfiguredControls { get; private set; }
    protected bool HasConfiguredContentControls { get; private set; }

    private readonly IMarkupActionRegistry? _actionRegistry;
    private readonly IUiCommandRegistry? _commandRegistry;
    private readonly IUiStateStore? _stateStore;
    private readonly IControlRegistry? _controlRegistry;
    private readonly IThemeRegistry? _themeRegistry;
    private readonly IWindowRegistry? _windowRegistry;
    private readonly List<IVisual> _configuredControls = [];
    private WindowConfig? _resolvedWindowConfig;
    private WindowTheme? _resolvedTheme;

    protected Window(IWindowHost host, ITrayService? tray, ILoggingService? log, IMarkupActionRegistry? actionRegistry = null, IUiCommandRegistry? commandRegistry = null, IUiStateStore? stateStore = null, IControlRegistry? controlRegistry = null, IThemeRegistry? themeRegistry = null, IWindowRegistry? windowRegistry = null) {
        _host = host;
        _tray = tray;
        _logger = log;
        _actionRegistry = actionRegistry;
        _commandRegistry = commandRegistry;
        _stateStore = stateStore;
        _controlRegistry = controlRegistry;
        _themeRegistry = themeRegistry;
        _windowRegistry = windowRegistry;

        _ctx = new WindowContext(
            target: host,
            uiThread: host,
            windowHost: host,
            new EventManager());
        if (_commandRegistry is not null)
            _ctx.SetCommandRegistry(_commandRegistry);
        if (_stateStore is not null)
            _ctx.SetStateStore(_stateStore);
        _ctx.SetOpenWindowAction(OpenWindowDefinition);

        InitializeFrame();
        InitializeRenderer();
        InitializeInteraction();
        InitializeConfiguredControls();
        RegisterWindowEvents();
        OnInitialize();
    }

    protected virtual string WindowDefinitionName => GetType().Name;
    protected virtual string WindowConfigPath => Paths.GetConfig("frame.yaml");

    protected virtual void InitializeFrame() {
        _resolvedWindowConfig = ResolveWindowConfig();
        _resolvedTheme = ResolveWindowTheme(_resolvedWindowConfig);

        var frameConfig = WindowDefinitionMerger.MergeFrame(_resolvedWindowConfig.Frame, _resolvedTheme);
        var frame = FrameResource.FromConfig(frameConfig, _ctx.Resources, _ctx.DpiScale);
        _ctx.SetFrame(frame);
    }

    protected virtual void InitializeRenderer() {
        _ctx.SetRenderer(new LayeredWindowRenderer(_ctx));
    }

    protected virtual void InitializeInteraction() {
        _interaction = new WindowInteraction(_host, _ctx);
    }

    protected virtual void RegisterWindowEvents() {
        _host.Shown += e => OnShown();
        _host.Closed += e => OnClosed(e.UserInitiated);
        _host.StateChanged += e => OnStateChanged(e.State);
        _host.FocusChanged += e => OnFocusChanged(e.State);
    }

    protected virtual void OnInitialize() {
        _logger?.Info($"{typeof(Window).FullName} OnInitialize()");
    }

    protected virtual async void OnShown() {
        _ctx.Events.NotifyAll(new WindowShownEvent());
        _logger?.Info($"{typeof(Window).FullName} OnShown()");
    }

    protected virtual void OnClosed(bool userClosing) {
        _ctx.Events.NotifyAll(new WindowClosedEvent(userClosing));
        _logger?.Info($"{typeof(Window).FullName} OnClosed({userClosing})");
    }

    protected virtual void OnStateChanged(WindowState state) {
        _ctx.Events.NotifyAll(new WindowStateChangedEvent(state));
        _logger?.Info($"{typeof(Window).FullName} OnStateChanged({state})");
    }

    protected virtual void OnFocusChanged(FocusState state) {
        _ctx.Events.NotifyAll(new WindowFocusChangedEvent(state));
        _logger?.Info($"{typeof(Window).FullName} OnFocusChanged({state})");
    }

    protected virtual void InitializeConfiguredControls() {
        if (_actionRegistry is null || _controlRegistry is null || _resolvedWindowConfig is null)
            return;

        ClearConfiguredControls();
        HasConfiguredControls = false;
        HasConfiguredContentControls = false;

        foreach (var config in WindowDefinitionMerger.MergeControls(_resolvedWindowConfig.Controls, _resolvedTheme)) {
            var control = ControlFactory.Create(_controlRegistry, _actionRegistry, _ctx, config);
            _ctx.UIElementManager.Add(control);
            _configuredControls.Add(control);
            HasConfiguredControls = true;

            if (control.Layer == VisualLayer.Content)
                HasConfiguredContentControls = true;
        }

        _ctx.RequestRender();
    }

    private WindowConfig ResolveWindowConfig() {
        if (_windowRegistry?.TryGet(WindowDefinitionName, out var config) == true && config is not null)
            return config;

        return ConfigLoader.Load<WindowConfig>(WindowConfigPath);
    }

    private WindowTheme? ResolveWindowTheme(WindowConfig config) {
        if (!string.IsNullOrWhiteSpace(config.Theme) &&
            _themeRegistry?.TryGet(config.Theme, out var theme) == true) {
            return theme;
        }

        return null;
    }

    private void OpenWindowDefinition(string name) {
        if (_windowRegistry?.TryGet(name, out var config) != true || config is null)
            throw new InvalidOperationException($"No window definition named '{name}' is registered.");

        _resolvedWindowConfig = config;
        _resolvedTheme = ResolveWindowTheme(config);
        HasConfiguredControls = false;
        HasConfiguredContentControls = false;

        var frameConfig = WindowDefinitionMerger.MergeFrame(config.Frame, _resolvedTheme);
        var frame = FrameResource.FromConfig(frameConfig, _ctx.Resources, _ctx.DpiScale);
        _ctx.SetFrame(frame);
        InitializeConfiguredControls();
    }

    private void ClearConfiguredControls() {
        foreach (var visual in _configuredControls) {
            _ctx.UIElementManager.Remove(visual);
        }

        _configuredControls.Clear();
    }

    public virtual void Dispose() {
        _logger?.Info($"{typeof(Window).FullName} Dispose()");
        OnClosed(userClosing: false);
        _tray?.Dispose();
        _ctx.Dispose();
    }
    public void RaiseClosed(bool userClosing) {
        _logger?.Info($"{typeof(Window).FullName} RaiseClosed({userClosing})");
        OnClosed(userClosing);
    }
}
