// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Backend.BackendAbstractions;
using KUpdater.Core;
using KUpdater.Core.Configuration;
using KUpdater.Core.Event;
using KUpdater.Core.Interop;
using KUpdater.Core.Pipeline;
using KUpdater.Core.Update;
using KUpdater.UI.Elements.Panel;
using KUpdater.UI.Layout;
using KUpdater.UI.Markup;
using KUpdater.UI.Platform;
using KUpdater.UI.Rendering;
using KUpdater.UI.Themes;
using KUpdater.Utility;

namespace KUpdater;

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

        var config = ConfigLoader.Load<WindowConfig>(Paths.GetConfig("frame.yaml"));
        var frameResources = FrameResource.FromConfig(config.Frame, _ctx.Resources, (_ctx.Target.DeviceDpi / 96f));
        _ctx.SetFrame(frameResources);


        var grid = new Grid(_ctx, "grid");

        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(150) });
        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });

        grid.Rows.Add(new RowDefinition { Height = GridLength.Pixel(50) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

        var header = new UI.Elements.Label(_ctx, "header", "HEADER", 10) {
            GridRow = 0,
            GridColumn = 0,
            GridColumnSpan = 2
        };

        var sidebar = new UI.Elements.Label(_ctx, "sidebar", "SIDEBAR", 10) {
            GridRow = 1,
            GridColumn = 0
        };

        var content = new UI.Elements.Label(_ctx, "content", "CONTENT", 10) {
            GridRow = 1,
            GridColumn = 1
        };

        grid.AddChild(header);
        grid.AddChild(sidebar);
        grid.AddChild(content);

        var btn_exit = new UI.Elements.Button(_ctx, "btn_exit", "X") {
            GridRow = 1,
            GridColumn = 1,
            Padding = new Thickness(100),
            Margin = new Thickness(100)
        };
        btn_exit.Click += () => {
            Debug.WriteLine("OK gedrückt");
        };


        grid.AddChild(btn_exit);
        _ctx.UIElementManager.Add(grid);


        var renderer = new LayeredWindowRenderer(_ctx);
        _ctx.SetRenderer(renderer);


        var pipeline = new UpdaterPipelineRunner(
            _ctx.Events,
            new HttpUpdateSource(),
            _ctx.Config.Updater.Url,
            AppDomain.CurrentDomain.BaseDirectory);

        _ctx.SetPipeline(pipeline);

        _interaction = new WindowInteraction(_backend, _ctx);

        _trayIcon = new TrayIcon()
            .Name("kUpdater")
            .Icon(Paths.GetResource("Default/app.ico"))
            .StatusIcons(status => status
                .Item("default", Paths.GetResource("Default/app.ico")))
            .Menu(menu => menu
                .Item("Debug", debug => debug
                    .Item("ContentRect", (s, e) => {
                        _ctx.Renderer.ToggleContentRectDebug();
                        _ctx.Renderer.RequestRender();
                    })
                    .Item("GridOverlay", (s, e) => {
                        _ctx.Renderer.ToggleDebugOverlay();
                        _ctx.Renderer.RequestRender();
                    })
                    .Item("PerfOverlay", (s, e) => {
                        _ctx.Renderer.TogglePerfOverlay();
                        _ctx.Renderer.RequestRender();
                    })
                    .Item("Toggle Bounds", (s, e) => {
                        DebugOverlay.Toggle(DebugOverlay.OverlayType.Bounds);
                        _ctx.Renderer.RequestRender();
                    })
                    .Item("Toggle LayoutRect", (s, e) => {
                        DebugOverlay.Toggle(DebugOverlay.OverlayType.LayoutRect);
                        _ctx.Renderer.RequestRender();
                    })
                    .Item("Toggle Meta", (s, e) => {
                        DebugOverlay.Toggle(DebugOverlay.OverlayType.Meta);
                        _ctx.Renderer.RequestRender();
                    })
                    .Item("Toggle ParentChain", (s, e) => {
                        DebugOverlay.Toggle(DebugOverlay.OverlayType.ParentChain);
                        _ctx.Renderer.RequestRender();
                    })
                )
                .Separator()
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
