// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;
using Kx.Sdk.Logging;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.Themes;
using Kx.Sdk.WindowHost;
using Kx.UI.Elements.Panel;
using Kx.UI.Layout;
using Kx.UI.Platform;

namespace Kx.Example.App;

public sealed class MainWindow : Window {
    public MainWindow(IWindowHost host, ITrayService tray, ILoggingService log, IMarkupActionRegistry actionRegistry, IUiCommandRegistry commandRegistry, IUiStateStore stateStore, IControlRegistry controlRegistry, IWindowFrameRegistry windowFrameRegistry, IWindowContentRegistry windowContentRegistry)
        : base(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, windowFrameRegistry, windowContentRegistry) {
    }

    protected override string? WindowIconResource => "Icons:app.ico";

    protected override void OnInitialize() {
        base.OnInitialize();

        if (HasConfiguredControls)
            return;

        BuildFallbackUi();
    }

    private void BuildFallbackUi() {
        _logger?.Info($"{typeof(MainWindow).FullName} BuildFallbackUi()");
        var grid = new Grid(_ctx, id: "example_grid");

        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new UI.Elements.Label(_ctx, id: "example_title", text: "Example App Fallback UI", size: 14) {
            GridRow = 0,
            GridColumn = 0,
            Margin = new Thickness(24)
        };

        var info = new UI.Elements.Label(_ctx, id: "example_info", text: "If the example plugin is loaded, its YAML window definition replaces this backup UI.", size: 10) {
            GridRow = 1,
            GridColumn = 0,
            Margin = new Thickness(24)
        };

        grid.AddChild(title);
        grid.AddChild(info);
        _ctx.UIElementManager.Add(grid);
    }
}
