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

namespace KxEdit;

public sealed class MainWindow : Window {
    public MainWindow(IWindowHost host, ITrayService tray, ILoggingService log, IMarkupActionRegistry actionRegistry, IUiCommandRegistry commandRegistry, IUiStateStore stateStore, IControlRegistry controlRegistry, IThemeRegistry themeRegistry, IWindowRegistry windowRegistry)
        : base(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, themeRegistry, windowRegistry) {
    }

    protected override string? WindowIconResource => "Icons:app.ico";

    protected override void OnInitialize() {
        base.OnInitialize();

        if (HasConfiguredControls)
            return;

        BuildUi();
    }

    private void BuildUi() {
        _logger?.Info($"{typeof(MainWindow).FullName} BuildUi()");

        // Layout: Toolbar (Auto), Editor (Star), Status (Auto)
        var grid = new Grid(_ctx, "main_grid");
        grid.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Pixel(32) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });
        grid.Rows.Add(new RowDefinition { Height = GridLength.Pixel(24) });

        // Toolbar (left-to-right)
        var toolbar = new StackPanel(_ctx, "toolbar") {
            Orientation = Kx.UI.Layout.Orientation.Horizontal,
            Spacing = 8,
            GridRow = 0,
            GridColumn = 0,
            Margin = new Thickness(8, 6, 8, 6)
        };

        var openButton = new Kx.UI.Elements.Button(_ctx, "btn_open", "Open");
        var saveButton = new Kx.UI.Elements.Button(_ctx, "btn_save", "Save");

        toolbar.AddChild(openButton);
        toolbar.AddChild(saveButton);

        // Two-column layout: TreeView (left), Editor (right)
        var contentGrid = new Grid(_ctx, "content_grid") {
            GridRow = 1,
            GridColumn = 0
        };
        contentGrid.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(260) });
        contentGrid.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });

        var tree = new Kx.UI.Elements.TreeView(_ctx, "config_tree") {
            GridRow = 0,
            GridColumn = 0,
            Margin = new Thickness(8)
        };

        var textBox = new Kx.UI.Elements.TextBox(_ctx, "config_text", string.Empty) {
            GridRow = 0,
            GridColumn = 1,
            Margin = new Thickness(12),
            Multiline = true,
            ReadOnly = false
        };

        // Status label
        var statusBar = new Kx.UI.Elements.Label(_ctx, "status_bar", "Bereit", 10) {
            GridRow = 2,
            GridColumn = 0,
            Margin = new Thickness(8, 0, 8, 4)
        };

        // Wire button actions
        openButton.Click += () => {
            try {
                using var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == DialogResult.OK) {
                    // If package contains multiple files, TreeView will list entries and textbox shows selected file(s)
                    var combined = PackageLoader.LoadAsText(dlg.FileName);
                    textBox.Text = combined;
                    // Populate tree with top-level entries if it's a zip
                    try {
                        using var s = File.OpenRead(dlg.FileName);
                        if (PackageLoader.IsZipStream(s)) {
                            s.Seek(0, SeekOrigin.Begin);
                            using var archive = new System.IO.Compression.ZipArchive(s, System.IO.Compression.ZipArchiveMode.Read, true);
                            var roots = new List<Kx.UI.Elements.TreeView.Node>();
                            foreach (var entry in archive.Entries) {
                                var parts = entry.FullName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                                AddToTree(roots, parts, 0);
                            }
                            tree.SetNodes(roots);
                        }
                    }
                    catch { }

                    statusBar.Text.Value = $"Loaded: {Path.GetFileName(dlg.FileName)}";
                    _logger?.Info($"Loaded file {dlg.FileName}");
                }
            }
            catch (Exception ex) {
                _logger?.Error($"Open failed: {ex.Message}", ex);
                statusBar.Text.Value = "Error loading file";
            }
        };

        saveButton.Click += () => {
            try {
                using var dlg = new SaveFileDialog();
                if (dlg.ShowDialog() == DialogResult.OK) {
                    File.WriteAllText(dlg.FileName, textBox.Text);
                    statusBar.Text.Value = $"Saved: {Path.GetFileName(dlg.FileName)}";
                    _logger?.Info($"Saved file {dlg.FileName}");
                }
            }
            catch (Exception ex) {
                _logger?.Error($"Save failed: {ex.Message}", ex);
                statusBar.Text.Value = "Error saving file";
            }
        };

        grid.AddChild(toolbar);
        contentGrid.AddChild(tree);
        contentGrid.AddChild(textBox);
        grid.AddChild(contentGrid);
        grid.AddChild(statusBar);

        _ctx.UIElementManager.Add(grid);
    }

    // Helper to build tree nodes from path parts
    static void AddToTree(List<Kx.UI.Elements.TreeView.Node> roots, string[] parts, int idx) {
        if (idx >= parts.Length)
            return;
        var name = parts[idx];
        var node = roots.Find(n => n.Name == name);
        if (node is null) {
            node = new Kx.UI.Elements.TreeView.Node(name);
            roots.Add(node);
        }
        AddToTree(node.Children, parts, idx + 1);
    }
}
