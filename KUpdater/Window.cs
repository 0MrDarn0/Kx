// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.Backend;
using KUpdater.Core;
using KUpdater.Core.Configuration;
using KUpdater.Core.Event;
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
    private readonly ITrayService? _trayService;

    public Window(IWindowBackend backend, ITrayService? trayService = null) {
        Instance = this;
        _backend = backend;
        _trayService = trayService;

        _ctx = new WindowContext(
            target: backend,
            uiThread: backend,
            backend: backend,
            eventManager: new EventManager());

        var config = ConfigLoader.Load<WindowConfig>(Paths.GetConfig("frame.yaml"));
        var frameResources = FrameResource.FromConfig(config.Frame, _ctx.Resources, (_ctx.Target.DeviceDpi / 96f));
        _ctx.SetFrame(frameResources);


        var grid = new Grid(_ctx, id: "grid");

        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(150) });
        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });

        grid.Rows.Add(new RowDefinition { Height = GridLength.Pixel(50) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

        var header = new UI.Elements.Label(_ctx, id : "header", text : "HEADER", size : 10) {
            GridRow = 0,
            GridColumn = 0,
            GridColumnSpan = 2
        };

        var sidebar = new UI.Elements.Label(_ctx, id: "sidebar", text: "SIDEBAR", size: 10) {
            GridRow = 1,
            GridColumn = 0
        };

        var content = new UI.Elements.Label(_ctx, id: "content", text: "CONTENT", size: 10) {
            GridRow = 1,
            GridColumn = 1
        };

        grid.AddChild(header);
        grid.AddChild(sidebar);
        grid.AddChild(content);

        var btn_exit = new UI.Elements.Button(_ctx, id: "btn_exit", text: "X") {
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


        var renderer = new LayeredWindowRenderer(ctx: _ctx);
        _ctx.SetRenderer(renderer);


        var pipeline = new UpdaterPipelineRunner(
            eventManager: _ctx.Events,
            source: new HttpUpdateSource(),
            baseUrl: _ctx.Config.Updater.Url,
            rootDir: AppDomain.CurrentDomain.BaseDirectory);

        _ctx.SetPipeline(pipeline);

        _interaction = new WindowInteraction(backend: _backend, ctx: _ctx);


        // TrayService konfigurieren, falls vorhanden
        _trayService?.Configure(t => t
                    .Name("kUpdater")
                    .Icon(Paths.GetResource("Default/app.ico"))
                    .StatusIcons(buildAction: status => status
                        .Item("default", Paths.GetResource("Default/app.ico")))
                    .Menu(buildAction: menu => menu
                        .Item("Debug", debug => debug
                            .Item("ContentRect", onClick: (s, e) => {
                                _ctx.Renderer.ToggleContentRectDebug();
                                _ctx.Renderer.RequestRender();
                            })
                            .Item("GridOverlay", onClick: (s, e) => {
                                _ctx.Renderer.ToggleDebugOverlay();
                                _ctx.Renderer.RequestRender();
                            })
                            .Item("PerfOverlay", onClick: (s, e) => {
                                _ctx.Renderer.TogglePerfOverlay();
                                _ctx.Renderer.RequestRender();
                            })
                            .Item("Toggle Bounds", onClick: (s, e) => {
                                DebugOverlay.Toggle(DebugOverlay.OverlayType.Bounds);
                                _ctx.Renderer.RequestRender();
                            })
                            .Item("Toggle LayoutRect", onClick: (s, e) => {
                                DebugOverlay.Toggle(DebugOverlay.OverlayType.LayoutRect);
                                _ctx.Renderer.RequestRender();
                            })
                            .Item("Toggle Meta", onClick: (s, e) => {
                                DebugOverlay.Toggle(DebugOverlay.OverlayType.Meta);
                                _ctx.Renderer.RequestRender();
                            })
                            .Item("Toggle ParentChain", onClick: (s, e) => {
                                DebugOverlay.Toggle(DebugOverlay.OverlayType.ParentChain);
                                _ctx.Renderer.RequestRender();
                            })
                        )
                        .Separator()
                        .Exit((s, e) => Application.Exit())
                    )
                );
    }


    public async void OnShown() {
        _ctx.Events.NotifyAll(new MainWindow_OnShown());

        if (_ctx.Pipeline != null)
            await _ctx.Pipeline.RunAsync(rootDir: AppDomain.CurrentDomain.BaseDirectory);
    }

    public void OnClosed(bool userClosing) {
        _ctx.Events.NotifyAll(new MainWindow_OnFormClosed(userClosing));

        // TrayService wird disposed, falls es nicht vom DI Container verwaltet wird.
        _trayService?.Dispose();

        _ctx.Dispose();
        Instance = null;
    }

    public void Dispose() {
        OnClosed(userClosing: false);
    }
}
