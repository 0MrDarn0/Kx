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

using SkiaSharp;

using KxButton = Kx.UI.Elements.Button;
using KxLabel = Kx.UI.Elements.Label;
using KxListBox = Kx.UI.Elements.ListBox;
using KxTextBox = Kx.UI.Elements.TextBox;

namespace KxUpdateBuilder;

public sealed class MainWindow(IWindowHost host, ITrayService tray, ILoggingService log, IMarkupActionRegistry actionRegistry, IUiCommandRegistry commandRegistry, IUiStateStore stateStore, IControlRegistry controlRegistry, IWindowFrameRegistry windowFrameRegistry, IWindowContentRegistry windowContentRegistry) : Window(host, tray, log, actionRegistry, commandRegistry, stateStore, controlRegistry, windowFrameRegistry, windowContentRegistry) {

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
    private KxTextBox? _newsTitleTextBox;
    private KxTextBox? _newsContentTextBox;
    private KxListBox? _newsEntriesListBox;
    private KxLabel? _statusLabel;
    private KxButton? _overwriteToggleButton;
    private KxButton? _buildButton;
    private KxButton? _openUpdateFolderButton;
    private KxButton? _openUploadFolderButton;
    private KxButton? _addNewsButton;
    private KxButton? _updateNewsButton;
    private KxButton? _removeNewsButton;
    private bool _overwriteExisting;
    private bool _buildInProgress;
    private List<UpdateNewsEntry> _newsEntries = [];
    private int _selectedNewsIndex = -1;

    private sealed record BuilderUiControls(
        KxTextBox UpdateFolderTextBox,
        KxTextBox UploadFolderTextBox,
        KxTextBox OutputTextBox,
        KxTextBox NewsTitleTextBox,
        KxTextBox NewsContentTextBox,
        KxListBox NewsEntriesListBox,
        KxLabel StatusLabel,
        KxButton OpenUpdateFolderButton,
        KxButton OpenUploadFolderButton,
        KxButton OverwriteToggleButton,
        KxButton BuildButton,
        KxButton AddNewsButton,
        KxButton UpdateNewsButton,
        KxButton RemoveNewsButton);

    protected override void OnInitialize() {
        base.OnInitialize();

        if (HasConfiguredControls)
            return;

        BuildUi();
        ApplyDefaults();
    }

    private void BuildUi() {
        Grid root = CreateRootGrid();
        BuilderUiControls controls = CreateUiControls();

        controls.NewsEntriesListBox.SelectedIndexChanged += OnNewsEntrySelectionChanged;

        AssignUiReferences(controls);
        AddUiChildren(root, controls);

        _ctx.UIElementManager.Add(root);
    }

    private Grid CreateRootGrid() {
        Grid root = new(_ctx, id: "builder_root") {
            Margin = new Thickness(18)
        };

        AddColumns(root, GridLength.Pixel(160), GridLength.Star(1), GridLength.Pixel(130));
        AddRows(
            root,
            GridLength.Pixel(42),
            GridLength.Pixel(34),
            GridLength.Pixel(40),
            GridLength.Pixel(40),
            GridLength.Pixel(42),
            GridLength.Pixel(40),
            GridLength.Pixel(90),
            GridLength.Pixel(28),
            GridLength.Pixel(130),
            GridLength.Pixel(40),
            GridLength.Pixel(30),
            GridLength.Star(1));

        return root;
    }

    private BuilderUiControls CreateUiControls() {
        KxTextBox updateFolderTextBox = CreateInputTextBox("builder_update_folder", row: 2, column: 1);
        KxTextBox uploadFolderTextBox = CreateInputTextBox("builder_upload_folder", row: 3, column: 1);
        KxTextBox newsTitleTextBox = CreateInputTextBox("builder_news_title", row: 5, column: 1);
        KxTextBox newsContentTextBox = CreateNewsContentTextBox();
        newsContentTextBox.InGrid(6, 1);
        KxListBox newsEntriesListBox = CreateNewsEntriesListBox();
        newsEntriesListBox.InGrid(8, 1, 1, 1);
        KxTextBox outputTextBox = CreateOutputTextBox();
        outputTextBox.InGrid(11, 0, 1, 3);

        KxLabel statusLabel = CreateLabel("builder_status", "Ready.", 11, _secondaryTextColor, row: 10, column: 0, columnSpan: 3);
        KxButton openUpdateFolderButton = CreateActionButton("builder_open_update", "Open", row: 2, column: 2, OnOpenUpdateFolderClicked);
        KxButton openUploadFolderButton = CreateActionButton("builder_open_upload", "Open", row: 3, column: 2, OnOpenUploadFolderClicked);
        KxButton overwriteToggleButton = CreateActionButton("builder_overwrite_toggle", "Overwrite: Off", row: 4, column: 1, OnOverwriteToggleClicked);
        KxButton buildButton = CreateActionButton("builder_build", "Build Manifest", row: 4, column: 2, OnBuildClicked);
        KxButton addNewsButton = CreateActionButton("builder_news_add", "Add News", row: 6, column: 2, OnAddNewsClicked);
        KxButton updateNewsButton = CreateActionButton("builder_news_update", "Update Selected", row: 7, column: 2, OnUpdateNewsClicked);
        KxButton removeNewsButton = CreateActionButton("builder_news_remove", "Remove Selected", row: 8, column: 2, OnRemoveNewsClicked);

        return new BuilderUiControls(
            updateFolderTextBox,
            uploadFolderTextBox,
            outputTextBox,
            newsTitleTextBox,
            newsContentTextBox,
            newsEntriesListBox,
            statusLabel,
            openUpdateFolderButton,
            openUploadFolderButton,
            overwriteToggleButton,
            buildButton,
            addNewsButton,
            updateNewsButton,
            removeNewsButton);
    }

    private void AssignUiReferences(BuilderUiControls controls) {
        _updateFolderTextBox = controls.UpdateFolderTextBox;
        _uploadFolderTextBox = controls.UploadFolderTextBox;
        _outputTextBox = controls.OutputTextBox;
        _newsTitleTextBox = controls.NewsTitleTextBox;
        _newsContentTextBox = controls.NewsContentTextBox;
        _newsEntriesListBox = controls.NewsEntriesListBox;
        _statusLabel = controls.StatusLabel;
        _openUpdateFolderButton = controls.OpenUpdateFolderButton;
        _openUploadFolderButton = controls.OpenUploadFolderButton;
        _overwriteToggleButton = controls.OverwriteToggleButton;
        _buildButton = controls.BuildButton;
        _addNewsButton = controls.AddNewsButton;
        _updateNewsButton = controls.UpdateNewsButton;
        _removeNewsButton = controls.RemoveNewsButton;
    }

    private void AddUiChildren(Grid root, BuilderUiControls controls) {
        root.AddChild(CreateLabel("builder_title", "Kx Update Builder", 20, _accentColor, row: 0, column: 0, columnSpan: 3));
        root.AddChild(CreateLabel("builder_subtitle", "Mirror update files into the upload folder and generate a file-based update.json manifest.", 11, _secondaryTextColor, row: 1, column: 0, columnSpan: 3));
        root.AddChild(CreateFieldLabel("builder_update_folder_label", "Update folder", row: 2));
        root.AddChild(controls.UpdateFolderTextBox);
        root.AddChild(controls.OpenUpdateFolderButton);
        root.AddChild(CreateFieldLabel("builder_upload_folder_label", "Upload folder", row: 3));
        root.AddChild(controls.UploadFolderTextBox);
        root.AddChild(controls.OpenUploadFolderButton);
        root.AddChild(CreateFieldLabel("builder_overwrite_label", "Existing output", row: 4));
        root.AddChild(controls.OverwriteToggleButton);
        root.AddChild(controls.BuildButton);
        root.AddChild(CreateFieldLabel("builder_news_title_label", "News title", row: 5));
        root.AddChild(controls.NewsTitleTextBox);
        root.AddChild(CreateFieldLabel("builder_news_content_label", "News content", row: 6));
        root.AddChild(controls.NewsContentTextBox);
        root.AddChild(CreateFieldLabel("builder_news_entries_label", "News entries", row: 7));
        root.AddChild(controls.RemoveNewsButton);
        root.AddChild(controls.NewsEntriesListBox);
        root.AddChild(controls.AddNewsButton);
        root.AddChild(controls.UpdateNewsButton);
        root.AddChild(controls.StatusLabel);
        root.AddChild(controls.OutputTextBox);
    }

    private static void AddColumns(Grid root, params GridLength[] widths) {
        foreach (GridLength width in widths)
            root.Columns.Add(new ColumnDefinition { Width = width });
    }

    private static void AddRows(Grid root, params GridLength[] heights) {
        foreach (GridLength height in heights)
            root.Rows.Add(new RowDefinition { Height = height });
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
        AppendOutput("Click a news entry to load it into title/content for editing or deletion.");
        RefreshNewsEntries(selectFirst: true);
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
        GetRequiredButton(_addNewsButton).IsEnabled = !isBusy;
        GetRequiredButton(_updateNewsButton).IsEnabled = !isBusy;
        GetRequiredButton(_removeNewsButton).IsEnabled = !isBusy;
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

    private static KxListBox GetRequiredListBox(KxListBox? listBox) {
        return listBox ?? throw new InvalidOperationException("The list UI is not initialized.");
    }

    private KxLabel CreateFieldLabel(string id, string text, int row) {
        return CreateLabel(id, text, 12, _panelTextColor, row, 0, 1);
    }

    private KxLabel CreateLabel(string id, string text, float size, SKColor color, int row, int column, int columnSpan) {
        KxLabel label = new KxLabel(_ctx, id, text, size)
            .WithForeground(color);

        label.InGrid(row, column, 1, columnSpan)
            .WithMargin(0, 8, 8, 0);
        return label;
    }

    private KxTextBox CreateInputTextBox(string id, int row, int column) {
        KxTextBox textBox = new KxTextBox(_ctx, id, string.Empty) {
            Multiline = false,
            ReadOnly = false,
        }
            .WithForeground(_panelTextColor)
            .WithBorder(_inputBorderColor, 1)
            .WithBackground(new SKColor(0x21, 0x23, 0x29));

        textBox.InGrid(row, column)
            .WithMargin(0, 2, 10, 2);
        return textBox;
    }

    private KxTextBox CreateOutputTextBox() {
        KxTextBox textBox = new KxTextBox(_ctx, "builder_output", string.Empty) {
            ReadOnly = true,
            Multiline = true,
        }
            .WithForeground(_panelTextColor)
            .WithBorder(_inputBorderColor, 1)
            .WithBackground(new SKColor(0x19, 0x1B, 0x20));

        textBox.WithMargin(0, 6, 0, 0)
            .WithPadding(10);
        return textBox;
    }

    private KxTextBox CreateNewsContentTextBox() {
        KxTextBox textBox = new KxTextBox(_ctx, "builder_news_content", string.Empty) {
            Multiline = true,
            ReadOnly = false,
        }
            .WithForeground(_panelTextColor)
            .WithBorder(_inputBorderColor, 1)
            .WithBackground(new SKColor(0x21, 0x23, 0x29));

        textBox.WithMargin(0, 2, 10, 2)
            .WithPadding(8);
        return textBox;
    }

    private KxListBox CreateNewsEntriesListBox() {
        KxListBox listBox = new KxListBox(_ctx, "builder_news_entries") {
            SelectedItemColor = new SKColor(0x4A, 0x67, 0x91, 180),
            HoveredItemColor = new SKColor(0x30, 0x35, 0x40, 180),
            SelectedItemBorderColor = _accentColor,
            ScrollBarColor = _accentColor
        }
            .WithForeground(_panelTextColor)
            .WithBorder(_inputBorderColor, 1)
            .WithBackground(new SKColor(0x19, 0x1B, 0x20));

        listBox.WithMargin(0, 2, 0, 2);
        return listBox;
    }

    private KxButton CreateActionButton(string id, string text, int row, int column, Action onClick) {
        KxButton button = new KxButton(_ctx, id, text)
            .WithForeground(_panelTextColor)
            .WithDisabledForeground(_secondaryTextColor)
            .WithBorder(_inputBorderColor, 1)
            .WithButtonStates(_buttonBackgroundColor, _buttonHoverBackgroundColor, _buttonPressedBackgroundColor, _buttonDisabledBackgroundColor)
            .OnClick(onClick);

        button.InGrid(row, column)
            .WithMargin(0, 2, 0, 2)
            .WithPadding(10, 8, 10, 8);
        return button;
    }

    private void OnAddNewsClicked() {
        try {
            UpdateNewsEditResult result = _packageBuilder.AddNewsEntry(new UpdateNewsAddRequest(
                GetText(_uploadFolderTextBox),
                GetText(_newsTitleTextBox),
                GetText(_newsContentTextBox)));

            AppendOutput($"News entry added: {result.NewsFilePath}");
            AppendOutput($"News entries: {result.EntryCount}");
            SetStatus("News entry added.", isError: false);
            GetRequiredTextBox(_newsContentTextBox).Text = string.Empty;
            RefreshNewsEntries(selectFirst: false, preferredIndex: 0);
        }
        catch (Exception ex) {
            _logger?.Error("Failed to add news entry.", ex);
            AppendOutput($"ERROR: {ex.Message}");
            SetStatus(ex.Message, isError: true);
        }
    }

    private void OnUpdateNewsClicked() {
        if (_selectedNewsIndex < 0 || _selectedNewsIndex >= _newsEntries.Count) {
            SetStatus("Select a news entry first.", isError: true);
            return;
        }

        try {
            UpdateNewsEditResult result = _packageBuilder.UpdateNewsEntry(new UpdateNewsUpdateRequest(
                GetText(_uploadFolderTextBox),
                _selectedNewsIndex,
                GetText(_newsTitleTextBox),
                GetText(_newsContentTextBox)));

            AppendOutput($"News entry updated: {_selectedNewsIndex + 1}");
            AppendOutput($"News entries: {result.EntryCount}");
            SetStatus("News entry updated.", isError: false);
            RefreshNewsEntries(selectFirst: false, preferredIndex: _selectedNewsIndex);
        }
        catch (Exception ex) {
            _logger?.Error("Failed to update news entry.", ex);
            AppendOutput($"ERROR: {ex.Message}");
            SetStatus(ex.Message, isError: true);
        }
    }

    private void OnRemoveNewsClicked() {
        if (_selectedNewsIndex < 0 || _selectedNewsIndex >= _newsEntries.Count) {
            SetStatus("Select a news entry first.", isError: true);
            return;
        }

        try {
            int removedIndex = _selectedNewsIndex;
            UpdateNewsEditResult result = _packageBuilder.RemoveNewsEntry(new UpdateNewsRemoveAtRequest(
                GetText(_uploadFolderTextBox),
                removedIndex));

            AppendOutput($"Removed news entry: {removedIndex + 1}");
            AppendOutput($"News entries: {result.EntryCount}");
            SetStatus("News entry removed.", isError: false);
            RefreshNewsEntries(selectFirst: false, preferredIndex: removedIndex);
        }
        catch (Exception ex) {
            _logger?.Error("Failed to remove news entries.", ex);
            AppendOutput($"ERROR: {ex.Message}");
            SetStatus(ex.Message, isError: true);
        }
    }

    private void OnNewsEntrySelectionChanged(int selectedIndex, string? _) {
        _selectedNewsIndex = selectedIndex;

        if (selectedIndex < 0 || selectedIndex >= _newsEntries.Count)
            return;

        UpdateNewsEntry entry = _newsEntries[selectedIndex];
        GetRequiredTextBox(_newsTitleTextBox).Text = entry.Title;
        GetRequiredTextBox(_newsContentTextBox).Text = entry.Content;
        SetStatus($"Selected news entry #{selectedIndex + 1}.", isError: false);
        _ctx.RequestRender();
    }

    private void RefreshNewsEntries(bool selectFirst, int? preferredIndex = null) {
        UpdateNewsListResult result = _packageBuilder.LoadNewsEntries(new UpdateNewsLoadRequest(GetText(_uploadFolderTextBox)));
        _newsEntries = result.Entries.ToList();

        KxListBox newsEntriesListBox = GetRequiredListBox(_newsEntriesListBox);
        newsEntriesListBox.SetItems(_newsEntries.Select((entry, index) => $"{index + 1:00}. {GetDisplayTitle(entry)}"));

        if (_newsEntries.Count == 0) {
            _selectedNewsIndex = -1;
            GetRequiredTextBox(_newsTitleTextBox).Text = string.Empty;
            GetRequiredTextBox(_newsContentTextBox).Text = string.Empty;
            return;
        }

        int index = preferredIndex ?? (selectFirst ? 0 : _selectedNewsIndex);
        index = Math.Clamp(index, 0, _newsEntries.Count - 1);
        newsEntriesListBox.SetSelectedIndex(index);
    }

    private static string GetDisplayTitle(UpdateNewsEntry entry) {
        if (!string.IsNullOrWhiteSpace(entry.Title))
            return entry.Title;

        if (!string.IsNullOrWhiteSpace(entry.Content))
            return entry.Content.Split(Environment.NewLine, 2, StringSplitOptions.None)[0].Trim();

        return "<empty>";
    }
}
