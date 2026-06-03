// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;
using Kx.Sdk.Logging;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.Themes;
using Kx.Sdk.WindowHost;
using Kx.UI.Elements.Panel;
using Kx.UI.Layout;
using Kx.UI.Platform;

using SkiaSharp;

using Button = Kx.UI.Elements.Button;
using Label = Kx.UI.Elements.Label;
using ListBox = Kx.UI.Elements.ListBox;
using Orientation = Kx.UI.Layout.Orientation;
using TextBox = Kx.UI.Elements.TextBox;

namespace KxEdit;

public sealed class MainWindow : Window {
    private static readonly string[] _availableControlTypes = [
        "Label",
        "Button",
        "TextBox",
        "ListBox",
        "StackPanel",
        "Grid"
    ];

    private readonly MarkupEditorDocument _document = MarkupEditorDocument.CreateDefault(EditorDocumentKind.Content);

    private string? _currentFilePath;
    private MarkupEditorControl? _selectedControl;
    private bool _frameSelected = true;

    private ListBox? _availableControlsList;
    private EditorPreviewCanvas? _previewCanvas;
    private Label? _selectionLabel;
    private Label? _statusLabel;

    private Label? _field1Label;
    private Label? _field2Label;
    private Label? _field3Label;
    private Label? _field4Label;
    private Label? _field5Label;
    private Label? _field6Label;
    private Label? _boundsLabel;
    private Label? _propertiesLabel;

    private TextBox? _field1Input;
    private TextBox? _field2Input;
    private TextBox? _field3Input;
    private TextBox? _field4Input;
    private TextBox? _field5Input;
    private TextBox? _field6Input;
    private TextBox? _boundsXInput;
    private TextBox? _boundsYInput;
    private TextBox? _boundsWidthInput;
    private TextBox? _boundsHeightInput;
    private TextBox? _propertiesInput;
    private PropertyPanelBinder? _propertyPanelBinder;

    public MainWindow(
        IWindowHost host,
        ITrayService tray,
        ILoggingService log,
        IMarkupActionRegistry actionRegistry,
        IUiCommandRegistry commandRegistry,
        IUiStateStore stateStore,
        IControlRegistry controlRegistry,
        IWindowFrameRegistry windowFrameRegistry,
        IWindowContentRegistry windowContentRegistry)
        : base(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, windowFrameRegistry, windowContentRegistry) {
    }

    protected override string? WindowIconResource => "Icons:app.ico";

    protected override void OnInitialize() {
        base.OnInitialize();
        _host.SetSize(1600, 960);

        if (HasConfiguredControls)
            return;

        BuildEditorUi();
        SelectFrame();
        RefreshAll();
        SetStatus("Editor bereit.");
    }

    private void BuildEditorUi() {
        var root = new Grid(_ctx, "editor_root");
        root.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        root.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(360) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(44) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(28) });

        root.AddChild(BuildToolbar());
        root.AddChild(BuildPreviewPane());
        root.AddChild(BuildSidebar());
        root.AddChild(BuildStatusBar());

        _ctx.UIElementManager.Add(root);
    }

    private UIElement BuildToolbar() {
        var toolbar = new StackPanel(_ctx, "toolbar") {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            GridRow = 0,
            GridColumn = 0,
            GridColumnSpan = 2,
            Margin = new Thickness(12, 8, 12, 4)
        };

        toolbar.AddChild(CreateButton("btn_new", "Neu", NewDocument));
        toolbar.AddChild(CreateButton("btn_open", "Öffnen", OpenDocument));
        toolbar.AddChild(CreateButton("btn_save", "Speichern", () => SaveDocument(saveAs: false)));
        toolbar.AddChild(CreateButton("btn_save_as", "Speichern unter", () => SaveDocument(saveAs: true)));

        return toolbar;
    }

    private UIElement BuildPreviewPane() {
        var pane = new Grid(_ctx, "preview_pane") {
            GridRow = 1,
            GridColumn = 0,
            Margin = new Thickness(12, 8, 8, 8)
        };
        pane.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        pane.Rows.Add(new RowDefinition { Height = GridLength.Pixel(28) });
        pane.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

        var caption = CreateCaptionLabel("preview_caption", "Frame + Content Preview");
        caption.GridRow = 0;
        caption.GridColumn = 0;

        _previewCanvas = new EditorPreviewCanvas(_ctx, "preview_canvas") {
            GridRow = 1,
            GridColumn = 0,
            Margin = new Thickness(0, 6, 0, 0)
        };
        _previewCanvas.SelectionChanged += OnPreviewSelectionChanged;
        _previewCanvas.DocumentChanged += OnPreviewDocumentChanged;

        pane.AddChild(caption);
        pane.AddChild(_previewCanvas);
        return pane;
    }

    private UIElement BuildSidebar() {
        var sidebar = new Grid(_ctx, "sidebar") {
            GridRow = 1,
            GridColumn = 1,
            Margin = new Thickness(8, 8, 12, 8)
        };
        sidebar.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        sidebar.Rows.Add(new RowDefinition { Height = GridLength.Pixel(250) });
        sidebar.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

        sidebar.AddChild(BuildAvailableControlsPane());
        sidebar.AddChild(BuildPropertyPane());
        return sidebar;
    }

    private UIElement BuildAvailableControlsPane() {
        var pane = new Grid(_ctx, "available_controls_pane") {
            GridRow = 0,
            GridColumn = 0
        };
        pane.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        pane.Rows.Add(new RowDefinition { Height = GridLength.Pixel(28) });
        pane.Rows.Add(new RowDefinition { Height = GridLength.Pixel(170) });
        pane.Rows.Add(new RowDefinition { Height = GridLength.Pixel(36) });

        var caption = CreateCaptionLabel("available_caption", "Verfuegbare Controls");
        caption.GridRow = 0;

        _availableControlsList = new ListBox(_ctx, "available_controls") {
            GridRow = 1,
            FixedBounds = new Rectangle(0, 0, 330, 170),
            Margin = new Thickness(0, 6, 0, 0)
        };
        _availableControlsList.SetItems(_availableControlTypes);
        _availableControlsList.SetSelectedIndex(0, notify: false);

        var buttons = new StackPanel(_ctx, "available_buttons") {
            GridRow = 2,
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        buttons.AddChild(CreateButton("btn_add_control", "Neu im Preview", AddSelectedControlType));
        buttons.AddChild(CreateButton("btn_delete_control", "Auswahl löschen", DeleteSelectedControl));

        pane.AddChild(caption);
        pane.AddChild(_availableControlsList);
        pane.AddChild(buttons);
        return pane;
    }

    private UIElement BuildPropertyPane() {
        var pane = new StackPanel(_ctx, "property_pane") {
            GridRow = 1,
            GridColumn = 0,
            Orientation = Orientation.Vertical,
            Spacing = 5,
            Margin = new Thickness(0, 12, 0, 0)
        };

        pane.AddChild(CreateCaptionLabel("property_caption", "Frame / Content Properties"));

        _selectionLabel = CreateValueLabel("selection_label", "Auswahl: Frame");
        pane.AddChild(_selectionLabel);

        _field1Label = CreateValueLabel("field1_label", "Titel");
        _field1Input = CreateInput("field1_input", 330, 30);
        pane.AddChild(_field1Label);
        pane.AddChild(_field1Input);

        _field2Label = CreateValueLabel("field2_label", "FrameDefinition");
        _field2Input = CreateInput("field2_input", 330, 30);
        pane.AddChild(_field2Label);
        pane.AddChild(_field2Input);

        _field3Label = CreateValueLabel("field3_label", "Background");
        _field3Input = CreateInput("field3_input", 330, 30);
        pane.AddChild(_field3Label);
        pane.AddChild(_field3Input);

        _field4Label = CreateValueLabel("field4_label", "TitleBar");
        _field4Input = CreateInput("field4_input", 330, 30);
        pane.AddChild(_field4Label);
        pane.AddChild(_field4Input);

        _field5Label = CreateValueLabel("field5_label", "Border");
        _field5Input = CreateInput("field5_input", 330, 30);
        pane.AddChild(_field5Label);
        pane.AddChild(_field5Input);

        _field6Label = CreateValueLabel("field6_label", "OnClick");
        _field6Input = CreateInput("field6_input", 330, 30);
        pane.AddChild(_field6Label);
        pane.AddChild(_field6Input);

        _boundsLabel = CreateValueLabel("bounds_label", "Bounds X / Y / W / H");
        pane.AddChild(_boundsLabel);

        var boundsRow = new StackPanel(_ctx, "bounds_row") {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        _boundsXInput = CreateInput("bounds_x", 78, 30);
        _boundsYInput = CreateInput("bounds_y", 78, 30);
        _boundsWidthInput = CreateInput("bounds_width", 78, 30);
        _boundsHeightInput = CreateInput("bounds_height", 78, 30);
        boundsRow.AddChild(_boundsXInput);
        boundsRow.AddChild(_boundsYInput);
        boundsRow.AddChild(_boundsWidthInput);
        boundsRow.AddChild(_boundsHeightInput);
        pane.AddChild(boundsRow);

        _propertiesLabel = CreateValueLabel("properties_label", "Properties");
        _propertiesInput = CreateInput("properties_input", 330, 96, multiline: true);
        pane.AddChild(_propertiesLabel);
        pane.AddChild(_propertiesInput);

        _propertyPanelBinder = new PropertyPanelBinder(
            _selectionLabel,
            _field1Label,
            _field2Label,
            _field3Label,
            _field4Label,
            _field5Label,
            _field6Label,
            _boundsLabel,
            _propertiesLabel,
            _field1Input,
            _field2Input,
            _field3Input,
            _field4Input,
            _field5Input,
            _field6Input,
            _boundsXInput,
            _boundsYInput,
            _boundsWidthInput,
            _boundsHeightInput,
            _propertiesInput);

        pane.AddChild(CreateButton("btn_apply_properties", "Properties uebernehmen", ApplyPropertyPanel));
        return pane;
    }

    private UIElement BuildStatusBar() {
        _statusLabel = new Label(_ctx, "status_bar", "Bereit", 10) {
            GridRow = 2,
            GridColumn = 0,
            GridColumnSpan = 2,
            Margin = new Thickness(12, 4, 12, 0)
        };
        _statusLabel.Color.Value = new SKColor(205, 214, 226);
        return _statusLabel;
    }

    private void NewDocument() {
        _currentFilePath = null;
        _document.Reset(EditorDocumentKind.Content);
        SelectFrame();
        RefreshAll();
        SetStatus("Neues gemeinsames Frame+Content-Dokument angelegt.");
    }

    private void OpenDocument() {
        try {
            using var dialog = new System.Windows.Forms.OpenFileDialog {
                Filter = "YAML-Dateien (*.yaml;*.yml)|*.yaml;*.yml|Alle Dateien (*.*)|*.*",
                CheckFileExists = true,
                Title = "YAML-Markup öffnen"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            var loaded = MarkupEditorDocument.Load(dialog.FileName);
            _document.CopyFrom(loaded);
            _currentFilePath = dialog.FileName;
            SelectFrame();
            RefreshAll();
            SetStatus($"Datei geladen: {dialog.FileName}");
        }
        catch (Exception ex) {
            SetStatus($"Laden fehlgeschlagen: {ex.Message}");
        }
    }

    private void SaveDocument(bool saveAs) {
        try {
            var targetPath = _currentFilePath;
            if (saveAs || string.IsNullOrWhiteSpace(targetPath)) {
                using var dialog = new System.Windows.Forms.SaveFileDialog {
                    Filter = "YAML-Dateien (*.yaml)|*.yaml|YML-Dateien (*.yml)|*.yml",
                    Title = "Frame- und Content-YAML speichern",
                    FileName = "window_content.yaml"
                };

                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                targetPath = dialog.FileName;
            }

            var result = _document.SavePair(targetPath!);
            _currentFilePath = result.ContentPath;
            SetStatus($"Gespeichert: {Path.GetFileName(result.FramePath)} und {Path.GetFileName(result.ContentPath)}");
        }
        catch (Exception ex) {
            SetStatus($"Speichern fehlgeschlagen: {ex.Message}");
        }
    }

    private void AddSelectedControlType() {
        var type = GetSelectedAvailableControlType();
        if (string.IsNullOrWhiteSpace(type))
            return;

        var control = MarkupEditorControl.CreateDefault(type);
        control.Bounds.X = 40 + _document.Controls.Count * 12;
        control.Bounds.Y = 40 + _document.Controls.Count * 12;

        if (_selectedControl is not null && IsContainer(_selectedControl.Type))
            _selectedControl.Children.Add(control);
        else
            _document.Controls.Add(control);

        SelectControl(control);
        _previewCanvas?.ClampControlToAllowedArea(control);
        RefreshAll();
        SetStatus($"Neues {type}-Control hinzugefuegt.");
    }

    private void DeleteSelectedControl() {
        if (_selectedControl is null)
            return;

        var removedId = _selectedControl.Id;
        if (_document.RemoveControl(_selectedControl)) {
            SelectFrame();
            RefreshAll();
            SetStatus($"Control '{removedId}' gelöscht.");
        }
    }

    private void ApplyPropertyPanel() {
        if (_propertyPanelBinder is null)
            return;

        if (_frameSelected) {
            _propertyPanelBinder.Apply(_document, selectedControl: null, frameSelected: true);
            _previewCanvas?.LoadDocument(_document, selectedControl: null, frameSelected: true);
            RefreshPropertyPanel();
            SetStatus("Frame-Properties übernommen.");
            return;
        }

        if (_selectedControl is null)
            return;

        _propertyPanelBinder.Apply(_document, _selectedControl, frameSelected: false);

        _previewCanvas?.ClampControlToAllowedArea(_selectedControl);
        _previewCanvas?.LoadDocument(_document, _selectedControl, frameSelected: false);
        RefreshPropertyPanel();
        SetStatus($"Properties für '{_selectedControl.Id}' übernommen.");
    }

    private void OnPreviewSelectionChanged(MarkupEditorControl? control, bool frameSelected) {
        if (frameSelected)
            SelectFrame();
        else
            SelectControl(control);

        RefreshPropertyPanel();
    }

    private void OnPreviewDocumentChanged() {
        RefreshPropertyPanel();
        SetStatus("Preview-Änderung übernommen.");
    }

    private void RefreshAll() {
        RefreshPropertyPanel();
        _previewCanvas?.LoadDocument(_document, _selectedControl, _frameSelected);
    }

    private void RefreshPropertyPanel() {
        _propertyPanelBinder?.Refresh(_document, _selectedControl, _frameSelected);
    }

    private void SelectFrame() {
        _selectedControl = null;
        _frameSelected = true;
    }

    private void SelectControl(MarkupEditorControl? control) {
        _selectedControl = control;
        _frameSelected = control is null;
    }

    private string? GetSelectedAvailableControlType() {
        if (_availableControlsList is null)
            return _availableControlTypes.FirstOrDefault();

        var index = _availableControlsList.SelectedIndex;
        return index >= 0 && index < _availableControlTypes.Length
            ? _availableControlTypes[index]
            : _availableControlTypes.FirstOrDefault();
    }

    private void SetStatus(string message) {
        if (_statusLabel is not null)
            _statusLabel.Text.Value = message;
    }

    private static bool IsContainer(string controlType) {
        return controlType.Equals("Grid", StringComparison.OrdinalIgnoreCase)
            || controlType.Equals("StackPanel", StringComparison.OrdinalIgnoreCase);
    }

    private Button CreateButton(string id, string text, Action onClick) {
        var button = new Button(_ctx, id, text) {
            Padding = new Thickness(10, 6, 10, 6)
        };
        button.BackgroundColor = new SKColor(48, 98, 180);
        button.HoverBackgroundColor = new SKColor(65, 120, 210);
        button.PressedBackgroundColor = new SKColor(34, 82, 156);
        button.BorderColor = new SKColor(98, 148, 230);
        button.ForegroundColor = SKColors.White;
        button.Click += onClick;
        return button;
    }

    private Label CreateCaptionLabel(string id, string text) {
        var label = new Label(_ctx, id, text, 12) {
            Margin = new Thickness(2, 2, 0, 0)
        };
        label.Color.Value = new SKColor(255, 226, 148);
        return label;
    }

    private Label CreateValueLabel(string id, string text) {
        var label = new Label(_ctx, id, text, 10) {
            Margin = new Thickness(2, 1, 0, 0)
        };
        label.Color.Value = new SKColor(206, 214, 226);
        return label;
    }

    private TextBox CreateInput(string id, int width, int height, bool multiline = false) {
        var input = new TextBox(_ctx, id, string.Empty) {
            FixedBounds = new Rectangle(0, 0, width, height),
            Multiline = multiline,
            ReadOnly = false
        };
        input.BackgroundColor = new SKColor(14, 18, 24);
        input.BorderColor = new SKColor(74, 86, 102);
        input.ForegroundColor = new SKColor(230, 236, 245);
        input.ScrollBarColor = new SKColor(104, 116, 132, 180);
        return input;
    }

}
