// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
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
                .Item("Settings", (s, e) => { })
                .Separator()
                .Exit((s, e) => Application.Exit()));
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        _trayIcon?.Dispose();
        _ctx.Dispose();
        Instance = null;
        base.OnFormClosed(e);
    }

    protected override async void OnShown(EventArgs e) {
        base.OnShown(e);
        _ctx.Renderer.RequestRender();

        // Pipeline starten
        await _ctx.Pipeline.RunAsync(AppDomain.CurrentDomain.BaseDirectory);
    }

    protected override void RequestRender() => _ctx.Renderer.RequestRender();

    // Weiterleitung der Input-Hooks an ControlManager
    protected override bool OnChildMouseMove(MouseEventArgs e) => _ctx.Controls.MouseMove(e.Location);
    protected override bool OnChildMouseDown(MouseEventArgs e) => _ctx.Controls.MouseDown(e.Location);
    protected override bool OnChildMouseUp(MouseEventArgs e) => _ctx.Controls.MouseUp(e.Location);
    protected override bool OnChildMouseWheel(MouseEventArgs e) => _ctx.Controls.MouseWheel(e.Delta, e.Location);

}
