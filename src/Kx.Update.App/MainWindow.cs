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

namespace Kx.Update.App;

public sealed class MainWindow : Window {
    public MainWindow(IWindowHost host, ITrayService tray, ILoggingService log, IMarkupActionRegistry actionRegistry, IUiCommandRegistry commandRegistry, IUiStateStore stateStore, IControlRegistry controlRegistry, IThemeRegistry themeRegistry, IWindowRegistry windowRegistry)
        : base(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, themeRegistry, windowRegistry) {
    }

    protected override void OnInitialize() {
        base.OnInitialize();

        if (HasConfiguredControls)
            return;

        BuildUI();
    }

    private void BuildUI() {
        _logger?.Info($"{typeof(MainWindow).FullName} BuildUI()");
        var grid = new Grid(_ctx, id: "grid");

        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(150) });
        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });

        grid.Rows.Add(new RowDefinition { Height = GridLength.Pixel(50) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

        var header = new UI.Elements.Label(_ctx, id: "header", text: "HEADER", size: 10) {
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
            _logger?.Info($"{btn_exit.Id} Clicked!");
        };

        grid.AddChild(btn_exit);
        _ctx.UIElementManager.Add(grid);
    }
}
