// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Backend;
using KUpdater.Core;
using KUpdater.Core.Configuration;
using KUpdater.Core.Event;
using KUpdater.Core.Interop;
using KUpdater.Core.Pipeline;
using KUpdater.Core.Update;
using KUpdater.Scripting.Runtime;
using KUpdater.UI;
using KUpdater.UI.Control;
using KUpdater.UI.Interface;
using KUpdater.UI.Rendering;
using KUpdater.Utility;
using Button = KUpdater.UI.Control.Button;
using Label = KUpdater.UI.Control.Label;

namespace KUpdater;

public class Window : IDisposable {
    public static Window? Instance { get; private set; }

    private readonly IWindowBackend _backend;
    private readonly WindowContext _ctx;
    private readonly WindowInteraction _interaction;
    private readonly TrayIcon? _trayIcon;
    public HotkeyManager? _hotkeyManager;
    private int _toggleDebugOverlayHotkeyId;
    private readonly AppConfig _config;

    public Window(IWindowBackend backend, AppConfig config) {
        Instance = this;
        _backend = backend;
        _config = config;

        _ctx = new WindowContext(
            backend,
            backend,
            backend,
            eventManager: new EventManager());

        var frameConfig = new FrameConfig();
        var frame = FrameLoader.Load(frameConfig, _ctx.Resources);
        _ctx.SetFrame(frame);


        var renderer = new Renderer(_ctx);
        _ctx.SetRenderer(renderer);

        //IUiEngine engine;
        //try { engine = PluginLoader.Load<IUiEngine>(_config.Ui.Engine); }
        //catch { engine = PluginLoader.Load<IUiEngine>("DefaultUI"); }
        //engine.Initialize(_ctx);
        //engine.BuildMainWindow();

        var titleLabel = new Label(
            id: "lb_title",
            boundsFunc: () => new Rectangle(35, 0, 200, 40),
            text: "KUpdater",
            font: new Font("Chiller", 40, FontStyle.Italic),
            color: Color.Orange
            ) { Layer = ControlLayer.Frame };

        _ctx.Controls.Add(titleLabel);

        var button = new Button(
            id: "btn_exit",
            boundsFunc: () => {
                int x = _backend.Width - 34;
                int y = 16;
                return new Rectangle(x, y, 17, 17);
            },
            text: "",
            font: new Font("Arial", 12),
            color: Color.White,
            skinKey: "KalOnline:Buttons",
            onClick: () => _backend.CloseWindow()
        ){ Layer = ControlLayer.Frame };

        _ctx.Controls.Add(button);

        var btn = new Button(
            "btn_default",
            () => new Rectangle(0, 0, 150, 40),
            "Click me",
            new Font("Arial", 14),
            Color.White,
            "KalOnline:Buttons",
            () => Debug.WriteLine("Clicked!")
        );

        _ctx.ContentRoot.Children.Add(btn);


        var pipeline = new UpdaterPipelineRunner(
            _ctx.Events,
            new HttpUpdateSource(),
            _ctx.Config.Url,
            AppDomain.CurrentDomain.BaseDirectory);

        _ctx.SetPipeline(pipeline);

        _interaction = new WindowInteraction(_backend, _ctx);

        LuaHost.OnNotify += (level, message) => {
            _backend.BeginInvoke(() => new MessageBoxWindow(new WinFormsBackend(), level, message, new MessageBoxOptions { Buttons = ["Ok"] }).ShowDialog());
        };

        _trayIcon = new TrayIcon()
            .Name("kUpdater")
            .Icon(Paths.GetResource("Default/app.ico"))
            .StatusIcons(status => status
                .Item("default", Paths.GetResource("Default/app.ico")))
            .Menu(menu => menu
                .Item("Toggle ContentRect", (s, e) => {
                    _ctx.Renderer.ToggleContentRectDebug();
                    _ctx.Renderer.RequestRender();
                })
                .Item("Toggle GridOverlay", (s, e) => {
                    _ctx.Renderer.ToggleDebugOverlay();
                    _ctx.Renderer.RequestRender();
                })
                .Item("Toggle PerfOverlay", (s, e) => {
                    _ctx.Renderer.TogglePerfOverlay();
                    _ctx.Renderer.RequestRender();
                })
                .Exit((s, e) => Application.Exit())
                );
        HookHotkeys();
    }

    public async void OnShown() {
        _ctx.Events.NotifyAll(new MainWindow_OnShown());

        if (_ctx.Pipeline != null)
            await _ctx.Pipeline.RunAsync(AppDomain.CurrentDomain.BaseDirectory);
    }

    public void OnClosed(bool userClosing) {
        _ctx.Events.NotifyAll(new MainWindow_OnFormClosed(userClosing));
        _trayIcon?.Dispose();
        _ctx.Dispose();
        _backend.HotkeySink = null;
        _hotkeyManager?.Dispose();
        Instance = null;
    }

    private void HookHotkeys() {
        _backend.BeginInvoke(() => {
            _hotkeyManager = new HotkeyManager(_backend.Handle);
            _backend.HotkeySink = _hotkeyManager;
            _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;

            try {
                _toggleDebugOverlayHotkeyId = _hotkeyManager.Register(
                    NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                    Keys.Y);
            }
            catch (Exception ex) {
                Debug.WriteLine($"Hotkey registration failed: {ex.Message}");
            }
        });
    }

    private void HotkeyManager_HotkeyPressed(object? sender, HotkeyEventArgs e) {
        if (e.Id == _toggleDebugOverlayHotkeyId) {
        }
    }

    public void Dispose() {
        OnClosed(false);
    }
}
