// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Core;
using KUpdater.Core.Event;
using KUpdater.Interop;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Skin;
using KUpdater.UI;
using KUpdater.UI.Interface;
using KUpdater.Utility;

namespace KUpdater;

public partial class MainWindow : Window, IRenderTarget, IUiThreadInvoker {
    public static MainWindow? Instance { get; private set; }
    private readonly WindowContext _ctx;
    private readonly TrayIcon? _trayIcon;
    private HotkeyManager? _hotkeyManager;
    private int _toggleDebugOverlayHotkeyId;

    public MainWindow() {
        Instance = this;

        _ctx = new WindowContext(
            this,
            this,
            ctx => new MainWindowSkin(ctx),
            ctx => new Renderer(ctx));

        LuaHost.OnNotify += (level, message) => {
            BeginInvoke(() => MessageBox.Show(this, message, level, MessageBoxButtons.OK, MessageBoxIcon.Information));
        };

        InitializeComponent();

        _trayIcon = new TrayIcon()
            .Name("kUpdater")
            .Icon(Paths.Resource("Default/app.ico"))
            .StatusIcons(status => status
                .Item("default", Paths.Resource("Default/app.ico"))
            )
            .Menu(menu => menu
                //.Item("Settings", (s, e) => { })
                //.Separator()
                .Exit((s, e) => Application.Exit()));
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        _ctx.Events.NotifyAll(new MainWindow_OnFormClosed(e.CloseReason == CloseReason.UserClosing));
        _trayIcon?.Dispose();
        _ctx.Dispose();
        _hotkeyManager?.Dispose();
        Instance = null;
        base.OnFormClosed(e);
    }

    protected override async void OnShown(EventArgs e) {
        base.OnShown(e);
        _ctx.Events.NotifyAll(new MainWindow_OnShown());
        await _ctx.Pipeline.RunAsync(AppDomain.CurrentDomain.BaseDirectory);
    }

    protected override void RequestRender() => _ctx.Renderer.RequestRender();
    protected override bool OnChildMouseMove(MouseEventArgs e) => _ctx.Controls.MouseMove(e.Location);
    protected override bool OnChildMouseDown(MouseEventArgs e) => _ctx.Controls.MouseDown(e.Location);
    protected override bool OnChildMouseUp(MouseEventArgs e) => _ctx.Controls.MouseUp(e.Location);
    protected override bool OnChildMouseWheel(MouseEventArgs e) => _ctx.Controls.MouseWheel(e.Delta, e.Location);

    #region Hotkey

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);

        _hotkeyManager = new HotkeyManager(this.Handle);
        _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;

        try {
            // Ctrl+Shift+Y
            _toggleDebugOverlayHotkeyId = _hotkeyManager.Register(NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, Keys.Y);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Hotkey registration failed: {ex.Message}");
        }
    }

    private void HotkeyManager_HotkeyPressed(object? sender, HotkeyEventArgs e) {
        if (e.Id == _toggleDebugOverlayHotkeyId) {
            (_ctx.Renderer as Renderer)?.ToggleDebugOverlay();
            RequestRender();
        }
    }

    protected override void WndProc(ref Message m) {
        // HotkeyManager verarbeitet WM_HOTKEY falls registriert
        if (_hotkeyManager != null && _hotkeyManager.ProcessWndProc(ref m))
            return;

        base.WndProc(ref m);
    }

    #endregion

}
