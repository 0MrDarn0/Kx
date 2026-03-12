// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.Events;
using Kx.Abstractions.Logging;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.Themes;
using Kx.Abstractions.UI.VisualTree;
using Kx.Abstractions.WindowHost;
using Kx.Core.Configuration;
using Kx.Core.Event;
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
    protected bool HasConfiguredContentControls { get; private set; }

    private readonly IControlRegistry? _controlRegistry;
    private readonly IThemeRegistry? _themeRegistry;
    private readonly IWindowRegistry? _windowRegistry;
    private WindowConfig? _resolvedWindowConfig;
    private WindowTheme? _resolvedTheme;

    protected Window(IWindowHost host, ITrayService? tray, ILoggingService? log, IControlRegistry? controlRegistry = null, IThemeRegistry? themeRegistry = null, IWindowRegistry? windowRegistry = null) {
        _host = host;
        _tray = tray;
        _logger = log;
        _controlRegistry = controlRegistry;
        _themeRegistry = themeRegistry;
        _windowRegistry = windowRegistry;

        _ctx = new WindowContext(
            target: host,
            uiThread: host,
            windowHost: host,
            new EventManager());

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

        var frameConfig = ResolveFrameConfig(_resolvedWindowConfig, _resolvedTheme);
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
        if (_controlRegistry is null || _resolvedWindowConfig is null)
            return;

        foreach (var config in ResolveControlConfigs(_resolvedWindowConfig, _resolvedTheme)) {
            var control = ControlFactory.Create(_controlRegistry, _ctx, config);
            _ctx.UIElementManager.Add(control);

            if (control.Layer == VisualLayer.Content)
                HasConfiguredContentControls = true;
        }
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

    private static FrameConfig ResolveFrameConfig(WindowConfig config, WindowTheme? theme) {
        if (theme is not null)
            return theme.Frame;

        return config.Frame;
    }

    private static IEnumerable<ControlConfig> ResolveControlConfigs(WindowConfig config, WindowTheme? theme) {
        if (theme?.Controls is not null) {
            foreach (var themeControl in theme.Controls)
                yield return themeControl;
        }

        foreach (var control in config.Controls)
            yield return control;
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
