// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

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

using KxButton = Kx.UI.Elements.Button;
using KxLabel = Kx.UI.Elements.Label;
using KxTextBox = Kx.UI.Elements.TextBox;

using SkiaSharp;

namespace KxUpdateBuilder;

public sealed class MainWindow : Window {
    private static readonly SKColor _panelTextColor = new(0xF5, 0xF5, 0xF5);
    private static readonly SKColor _secondaryTextColor = new(0xB7, 0xBC, 0xC6);
    private static readonly SKColor _accentColor = new(0x6C, 0xB2, 0xFF);
    private static readonly SKColor _errorTextColor = new(0xFF, 0x7B, 0x72);
    private static readonly SKColor _inputBorderColor = new(0x3A, 0x3D, 0x46);
    private static readonly SKColor _buttonBackgroundColor = new(0x2B, 0x2D, 0x34);
    private static readonly SKColor _buttonHoverBackgroundColor = new(0x35, 0x38, 0x41);
    private static readonly SKColor _buttonPressedBackgroundColor = new(0x40, 0x44, 0x4F);
    private static readonly SKColor _buttonDisabledBackgroundColor = new(0x26, 0x28, 0x2F);

    private readonly UpdatePackageBuilder _packageBuilder = new();

    private KxTextBox? _updateFolderTextBox;
    private KxTextBox? _uploadFolderTextBox;
    private KxTextBox? _outputTextBox;
    private KxLabel? _statusLabel;
    private KxButton? _overwriteToggleButton;
    private KxButton? _buildButton;
    private KxButton? _openUpdateFolderButton;
    private KxButton? _openUploadFolderButton;
    private bool _overwriteExisting;
    private bool _buildInProgress;

    public MainWindow(IWindowHost host, ITrayService tray, ILoggingService log, IMarkupActionRegistry actionRegistry, IUiCommandRegistry commandRegistry, IUiStateStore stateStore, IControlRegistry controlRegistry, IWindowFrameRegistry windowFrameRegistry, IWindowContentRegistry windowContentRegistry)
        : base(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, windowFrameRegistry, windowContentRegistry) {
    }

    protected override void OnInitialize() {
        base.OnInitialize();

        if (HasConfiguredControls)
            return;

        BuildUi();
        ApplyDefaults();
    }

    private void BuildUi() {
        Grid root = new(_ctx, id: "builder_root") {
            Margin = new Thickness(18)
        };

        root.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(160) });
        root.Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        root.Columns.Add(new ColumnDefinition { Width = GridLength.Pixel(130) });

        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(42) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(34) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(40) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(40) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(42) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(30) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Pixel(30) });
        root.Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

        KxTextBox updateFolderTextBox = CreateInputTextBox("builder_update_folder", row: 2, column: 1);
        KxTextBox uploadFolderTextBox = CreateInputTextBox("builder_upload_folder", row: 3, column: 1);
        KxTextBox outputTextBox = CreateOutputTextBox();
        outputTextBox.GridRow = 7;
        outputTextBox.GridColumn = 0;
        outputTextBox.GridColumnSpan = 3;

        KxLabel statusLabel = CreateLabel("builder_status", "Ready.", 11, _secondaryTextColor, row: 5, column: 0, columnSpan: 3);
        KxButton openUpdateFolderButton = CreateActionButton("builder_open_update", "Open", row: 2, column: 2, OnOpenUpdateFolderClicked);
        KxButton openUploadFolderButton = CreateActionButton("builder_open_upload", "Open", row: 3, column: 2, OnOpenUploadFolderClicked);
        KxButton overwriteToggleButton = CreateActionButton("builder_overwrite_toggle", "Overwrite: Off", row: 4, column: 1, OnOverwriteToggleClicked);
        KxButton buildButton = CreateActionButton("builder_build", "Build Manifest", row: 4, column: 2, OnBuildClicked);

        _updateFolderTextBox = updateFolderTextBox;
        _uploadFolderTextBox = uploadFolderTextBox;
        _outputTextBox = outputTextBox;
        _statusLabel = statusLabel;
        _openUpdateFolderButton = openUpdateFolderButton;
        _openUploadFolderButton = openUploadFolderButton;
        _overwriteToggleButton = overwriteToggleButton;
        _buildButton = buildButton;

        root.AddChild(CreateLabel("builder_title", "Kx Update Builder", 20, _accentColor, row: 0, column: 0, columnSpan: 3));
        root.AddChild(CreateLabel("builder_subtitle", "Mirror update files into the upload folder and generate a file-based update.json manifest.", 11, _secondaryTextColor, row: 1, column: 0, columnSpan: 3));
        root.AddChild(CreateFieldLabel("builder_update_folder_label", "Update folder", row: 2));
        root.AddChild(updateFolderTextBox);
        root.AddChild(openUpdateFolderButton);
        root.AddChild(CreateFieldLabel("builder_upload_folder_label", "Upload folder", row: 3));
        root.AddChild(uploadFolderTextBox);
        root.AddChild(openUploadFolderButton);
        root.AddChild(CreateFieldLabel("builder_overwrite_label", "Existing output", row: 4));
        root.AddChild(overwriteToggleButton);
        root.AddChild(buildButton);
        root.AddChild(statusLabel);
        root.AddChild(outputTextBox);

        _ctx.UIElementManager.Add(root);
    }

    private void ApplyDefaults() {
        UpdatePackageBuildDefaults defaults = _packageBuilder.CreateDefaults(Directory.GetCurrentDirectory());

        Directory.CreateDirectory(defaults.UpdateFolder);
        Directory.CreateDirectory(defaults.UploadFolder);

        GetRequiredTextBox(_updateFolderTextBox).Text = defaults.UpdateFolder;
        GetRequiredTextBox(_uploadFolderTextBox).Text = defaults.UploadFolder;

        AppendOutput($"Working directory: {Directory.GetCurrentDirectory()}");
        AppendOutput($"Update folder: {defaults.UpdateFolder}");
        AppendOutput($"Upload folder: {defaults.UploadFolder}");
        AppendOutput("Adjust the folders, then build the manifest.");
        SetStatus("Ready.", isError: false);
    }

    private void OnOpenUpdateFolderClicked() {
        OpenFolder(GetText(_updateFolderTextBox), "Update folder opened.");
    }

    private void OnOpenUploadFolderClicked() {
        OpenFolder(GetText(_uploadFolderTextBox), "Upload folder opened.");
    }

    private void OnOverwriteToggleClicked() {
        _overwriteExisting = !_overwriteExisting;
        GetRequiredButton(_overwriteToggleButton).Text = _overwriteExisting ? "Overwrite: On" : "Overwrite: Off";
        _ctx.RequestRender();
        SetStatus(_overwriteExisting ? "Existing output files will be overwritten." : "Existing output files will be preserved.", isError: false);
    }

    private async void OnBuildClicked() {
        if (_buildInProgress)
            return;

        try {
            _buildInProgress = true;
            SetBusyState(true);
            SetStatus("Building update manifest...", isError: false);

            UpdatePackageBuildRequest request = new(
                GetText(_updateFolderTextBox),
                GetText(_uploadFolderTextBox),
                _overwriteExisting);

            UpdatePackageBuildResult result = await Task.Run(() => _packageBuilder.Build(request));

            AppendOutput($"Manifest created: {result.OutputJson}");
            AppendOutput($"Mirrored files: {result.FileCount}");
            AppendOutput($"Deleted files: {result.DeletedFileCount}");
            if (result.OverwroteExistingFiles)
                AppendOutput("Existing output files were overwritten.");

            SetStatus("Update manifest created successfully.", isError: false);
        }
        catch (Exception ex) {
            _logger?.Error("Failed to build update manifest.", ex);
            AppendOutput($"ERROR: {ex.Message}");
            SetStatus(ex.Message, isError: true);
        }
        finally {
            _buildInProgress = false;
            SetBusyState(false);
        }
    }

    private void OpenFolder(string folderPath, string successMessage) {
        try {
            string normalizedPath = Path.GetFullPath(folderPath);
            Directory.CreateDirectory(normalizedPath);

            using Process process = Process.Start(new ProcessStartInfo {
                FileName = "explorer.exe",
                Arguments = normalizedPath,
                UseShellExecute = true
            }) ?? throw new InvalidOperationException("Could not start Explorer.");

            SetStatus(successMessage, isError: false);
        }
        catch (Exception ex) {
            _logger?.Error("Failed to open folder.", ex);
            AppendOutput($"ERROR: {ex.Message}");
            SetStatus(ex.Message, isError: true);
        }
    }

    private void SetBusyState(bool isBusy) {
        GetRequiredButton(_buildButton).IsEnabled = !isBusy;
        GetRequiredButton(_openUpdateFolderButton).IsEnabled = !isBusy;
        GetRequiredButton(_openUploadFolderButton).IsEnabled = !isBusy;
        GetRequiredButton(_overwriteToggleButton).IsEnabled = !isBusy;
        _ctx.RequestRender();
    }

    private void AppendOutput(string message) {
        KxTextBox outputTextBox = GetRequiredTextBox(_outputTextBox);
        string prefix = string.IsNullOrWhiteSpace(outputTextBox.Text) ? string.Empty : Environment.NewLine;
        outputTextBox.Text += prefix + $"[{DateTime.Now:HH:mm:ss}] {message}";
    }

    private void SetStatus(string message, bool isError) {
        KxLabel statusLabel = GetRequiredLabel(_statusLabel);
        statusLabel.Text.Value = message;
        statusLabel.Color.Value = isError ? _errorTextColor : _secondaryTextColor;
    }

    private static string GetText(KxTextBox? textBox) {
        return GetRequiredTextBox(textBox).Text;
    }

    private static KxTextBox GetRequiredTextBox(KxTextBox? textBox) {
        return textBox ?? throw new InvalidOperationException("The text box UI is not initialized.");
    }

    private static KxButton GetRequiredButton(KxButton? button) {
        return button ?? throw new InvalidOperationException("The button UI is not initialized.");
    }

    private static KxLabel GetRequiredLabel(KxLabel? label) {
        return label ?? throw new InvalidOperationException("The label UI is not initialized.");
    }

    private KxLabel CreateFieldLabel(string id, string text, int row) {
        return CreateLabel(id, text, 12, _panelTextColor, row, 0, 1);
    }

    private KxLabel CreateLabel(string id, string text, float size, SKColor color, int row, int column, int columnSpan) {
        KxLabel label = new(_ctx, id, text, size) {
            GridRow = row,
            GridColumn = column,
            GridColumnSpan = columnSpan,
            Margin = new Thickness(0, 8, 8, 0)
        };

        label.Color.Value = color;
        return label;
    }

    private KxTextBox CreateInputTextBox(string id, int row, int column) {
        return new KxTextBox(_ctx, id, string.Empty) {
            GridRow = row,
            GridColumn = column,
            Multiline = false,
            ReadOnly = false,
            Margin = new Thickness(0, 2, 10, 2),
            BorderThickness = 1,
            BorderColor = _inputBorderColor,
            ForegroundColor = _panelTextColor,
            BackgroundColor = new SKColor(0x21, 0x23, 0x29)
        };
    }

    private KxTextBox CreateOutputTextBox() {
        return new KxTextBox(_ctx, "builder_output", string.Empty) {
            ReadOnly = true,
            Multiline = true,
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(10),
            BorderThickness = 1,
            BorderColor = _inputBorderColor,
            ForegroundColor = _panelTextColor,
            BackgroundColor = new SKColor(0x19, 0x1B, 0x20)
        };
    }

    private KxButton CreateActionButton(string id, string text, int row, int column, Action onClick) {
        KxButton button = new(_ctx, id, text) {
            GridRow = row,
            GridColumn = column,
            Margin = new Thickness(0, 2, 0, 2),
            Padding = new Thickness(10, 8, 10, 8),
            ForegroundColor = _panelTextColor,
            BackgroundColor = _buttonBackgroundColor,
            HoverBackgroundColor = _buttonHoverBackgroundColor,
            PressedBackgroundColor = _buttonPressedBackgroundColor,
            DisabledBackgroundColor = _buttonDisabledBackgroundColor,
            DisabledForegroundColor = _secondaryTextColor,
            BorderColor = _inputBorderColor
        };

        button.Click += onClick;
        return button;
    }
}
