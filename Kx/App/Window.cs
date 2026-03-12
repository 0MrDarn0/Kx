// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.Events;
using Kx.Abstractions.Logging;
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

    protected Window(IWindowHost host, ITrayService? tray, ILoggingService? log) {
        _host = host;
        _tray = tray;
        _logger = log;

        _ctx = new WindowContext(
            target: host,
            uiThread: host,
            windowHost: host,
            new EventManager());

        InitializeFrame();
        InitializeRenderer();
        InitializeInteraction();
        RegisterWindowEvents();
        OnInitialize();
    }

    protected virtual void InitializeFrame() {
        var cfg = ConfigLoader.Load<WindowConfig>(Paths.GetConfig("frame.yaml"));
        var frame = FrameResource.FromConfig(cfg.Frame, _ctx.Resources, _ctx.DpiScale);
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
