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
    protected IUiStateStore StateStore => ((Kx.Sdk.UI.IVisualContext)_ctx).State;

    private bool _isInitialized;
    private readonly IMarkupActionRegistry? _actionRegistry;
    private readonly IUiCommandRegistry? _commandRegistry;
    private readonly IUiStateStore? _stateStore;
    private readonly IControlRegistry? _controlRegistry;
    private readonly IWindowFrameRegistry? _windowFrameRegistry;
    private readonly IWindowContentRegistry? _windowContentRegistry;
    private readonly List<IVisual> _configuredControls = [];
    private WindowContentDefinition? _resolvedWindowContentDefinition;
    private WindowFrameDefinition? _resolvedWindowFrameDefinition;

    protected Window(IWindowHost host, ITrayService? tray, ILoggingService? log, IMarkupActionRegistry? actionRegistry = null, IUiCommandRegistry? commandRegistry = null, IUiStateStore? stateStore = null, IControlRegistry? controlRegistry = null, IWindowFrameRegistry? windowFrameRegistry = null, IWindowContentRegistry? windowContentRegistry = null) {
        _host = host;
        _tray = tray;
        _logger = log;
        _actionRegistry = actionRegistry;
        _commandRegistry = commandRegistry;
        _stateStore = stateStore;
        _controlRegistry = controlRegistry;
        _windowFrameRegistry = windowFrameRegistry;
        _windowContentRegistry = windowContentRegistry;

        _ctx = new WindowContext(
            target: host,
            uiThread: host,
            windowHost: host,
            new EventManager());

        if (_commandRegistry is not null)
            _ctx.SetCommandRegistry(_commandRegistry);
        if (_stateStore is not null)
            _ctx.SetStateStore(_stateStore);
        _ctx.SetOpenWindowAction(OpenWindowContent);

        InitializeFrame();
        InitializeRenderer();
        InitializeInteraction();
        InitializeConfiguredControls();
        RegisterWindowEvents();
    }

    protected virtual string WindowContentDefinitionName => GetType().Name;
    protected virtual string WindowContentDefinitionPath => Paths.GetConfig("frame.yaml");
    protected virtual string? WindowIconResource => null;

    internal void InitializeWindow() {
        if (_isInitialized)
            return;

        _isInitialized = true;
        OnInitialize();
    }

    protected virtual void InitializeFrame() {
        _resolvedWindowContentDefinition = ResolveWindowContentDefinition();
        _resolvedWindowFrameDefinition = ResolveWindowFrameDefinition(_resolvedWindowContentDefinition);

        ApplyFrame(WindowCompositionMerger.MergeFrame(_resolvedWindowContentDefinition.Frame, _resolvedWindowFrameDefinition));
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

    protected virtual void OnShown() {
        _ = OnShownAsyncWrapper();
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

    private async Task OnShownAsyncWrapper() {
        try {
            await Task.Yield();
            _ctx.RequestRender();
            await Task.Run(() => _ctx.Events.NotifyAll(new WindowShownEvent())).ConfigureAwait(false);
            _logger?.Info($"{typeof(Window).FullName} OnShown()");
        }
        catch (Exception ex) {
            _logger?.Error("Unhandled exception in OnShown.", ex);
        }
    }

    protected virtual void InitializeConfiguredControls() {
        if (_actionRegistry is null || _controlRegistry is null || _resolvedWindowContentDefinition is null)
            return;

        ClearConfiguredControls();
        HasConfiguredControls = false;
        HasConfiguredContentControls = false;

        foreach (var config in WindowCompositionMerger.MergeControls(_resolvedWindowContentDefinition.Controls, _resolvedWindowFrameDefinition)) {
            var control = ControlFactory.Create(_controlRegistry, _actionRegistry, _ctx, config);
            _ctx.UIElementManager.Add(control);
            _configuredControls.Add(control);
            HasConfiguredControls = true;

            if (control.Layer == VisualLayer.Content)
                HasConfiguredContentControls = true;
        }

        _ctx.RequestRender();
    }

    private void ApplyFrame(FrameConfig frameConfig) {
        var frame = FrameResource.FromConfig(frameConfig, _ctx.Resources, _ctx.DpiScale);
        _ctx.SetFrame(frame);
        _ctx.SetWindowIcon(ResolveWindowIconResource(frameConfig));
    }

    private string? ResolveWindowIconResource(FrameConfig frameConfig) {
        if (!string.IsNullOrWhiteSpace(WindowIconResource))
            return WindowIconResource;

        if (!string.IsNullOrWhiteSpace(frameConfig.Default.Icon))
            return frameConfig.Default.Icon;

        return _ctx.Config.Window.Icon;
    }

    private WindowContentDefinition ResolveWindowContentDefinition() {
        if (_windowContentRegistry?.TryGet(WindowContentDefinitionName, out var contentDefinition) == true && contentDefinition is not null)
            return contentDefinition;

        return ConfigLoader.Load<WindowContentDefinition>(WindowContentDefinitionPath);
    }

    private WindowFrameDefinition? ResolveWindowFrameDefinition(WindowContentDefinition contentDefinition) {
        if (!string.IsNullOrWhiteSpace(contentDefinition.FrameDefinition) &&
            _windowFrameRegistry?.TryGet(contentDefinition.FrameDefinition, out var frameDefinition) == true) {
            return frameDefinition;
        }

        return null;
    }

    private void OpenWindowContent(string name) {
        if (_windowContentRegistry?.TryGet(name, out var contentDefinition) != true || contentDefinition is null)
            throw new InvalidOperationException($"No window definition named '{name}' is registered.");

        _resolvedWindowContentDefinition = contentDefinition;
        _resolvedWindowFrameDefinition = ResolveWindowFrameDefinition(contentDefinition);
        HasConfiguredControls = false;
        HasConfiguredContentControls = false;

        ApplyFrame(WindowCompositionMerger.MergeFrame(contentDefinition.Frame, _resolvedWindowFrameDefinition));
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
