// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Core;
using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Interop;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Skin;
using KUpdater.UI.Interface;
using KUpdater.UI.Rendering;
using KUpdater.Utility;

namespace KUpdater.UI;

public class Window : IDisposable {
    public static Window? Instance { get; private set; }

    private readonly IWindowBackend _backend;
    private readonly WindowContext _ctx;
    private readonly WindowInteraction _interaction;
    private readonly TrayIcon? _trayIcon;
    public HotkeyManager? _hotkeyManager;
    private int _toggleDebugOverlayHotkeyId;

    public Window(IWindowBackend backend) {
        Instance = this;
        _backend = backend;

        _ctx = new WindowContext(
            backend,
            backend,
            backend,
            eventManager: new EventManager());

        var skin = new MainSkin(_ctx);
        _ctx.SetSkin(skin);

        var renderer = new Renderer(_ctx);
        _ctx.SetRenderer(renderer);

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
            .Icon(Paths.Resource("Default/app.ico"))
            .StatusIcons(status => status
                .Item("default", Paths.Resource("Default/app.ico")))
            .Menu(menu => menu
                .Exit((s, e) => Application.Exit())
                .Item("GridOverlay", (s, e) => {
                    _ctx.Renderer.ToggleDebugOverlay();
                    _ctx.Renderer.RequestRender();
                })
                .Item("PerfOverlay", (s, e) => {
                    _ctx.Renderer.TogglePerfOverlay();
                    _ctx.Renderer.RequestRender();
                }
                ));

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


            //var msg = new MessageBoxWindow(new WinFormsBackend(), "Exit?", "Are you sure?", new MessageBoxOptions{ Buttons=["Yes", "No"] });
            //var result = msg.ShowDialog();

            //if (result == MessageBoxResult.No) {
            //    Application.Exit();
            //}

        }
    }

    public void Dispose() {
        OnClosed(false);
    }
}
