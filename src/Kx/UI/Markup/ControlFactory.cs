// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Binding;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.VisualTree;
using Kx.App;
using Kx.UI.State;
using Kx.UI.Elements.Panel;
using Kx.UI.Layout;

using SkiaSharp;

namespace Kx.UI.Markup;

public static class ControlFactory {
    public static UIElement Create(IControlRegistry registry, IMarkupActionRegistry actionRegistry, IVisualContext context, ControlConfig config) {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(actionRegistry);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(config);

        if (!registry.TryCreate(context, config, out var control) || control is null)
            throw new InvalidOperationException($"No control factory has been registered for type '{config.Type}'.");

        ApplyCommonProperties(actionRegistry, context, control, config);
        ApplyContainerProperties(registry, actionRegistry, context, control, config);
        return control;
    }

    private static void ApplyCommonProperties(IMarkupActionRegistry actionRegistry, IVisualContext visualContext, UIElement control, ControlConfig config) {
        if (config.Margin is not null)
            control.Margin = ToThickness(config.Margin);

        if (config.Padding is not null)
            control.Padding = ToThickness(config.Padding);

        if (Enum.TryParse<VisualLayer>(config.Layer, ignoreCase: true, out var layer)) {
            control.Layer = layer;
            control.UseContentArea = layer == VisualLayer.Content;
        }

        if (!string.IsNullOrWhiteSpace(config.Dock) &&
            Enum.TryParse<Dock>(config.Dock, ignoreCase: true, out var dock)) {
            control.Dock = dock;
        }

        if (config.Bounds is not null) {
            control.FixedBounds = new Rectangle(
                config.Bounds.X,
                config.Bounds.Y,
                config.Bounds.Width,
                config.Bounds.Height);
        }

        bool hasVisualOffsetX = TryGetIntProperty(config, "visualOffsetX", out int visualOffsetX);
        bool hasVisualOffsetY = TryGetIntProperty(config, "visualOffsetY", out int visualOffsetY);
        if (hasVisualOffsetX || hasVisualOffsetY)
            control.VisualOffset = new Point(visualOffsetX, visualOffsetY);

        control.GridRow = config.GridRow;
        control.GridColumn = config.GridColumn;
        control.GridRowSpan = Math.Max(1, config.GridRowSpan);
        control.GridColumnSpan = Math.Max(1, config.GridColumnSpan);

        switch (control) {
            case Kx.UI.Elements.Label label:
                ApplyLabelProperties(label, config);
                break;

            case Kx.UI.Elements.Button button:
                ApplyButtonProperties(actionRegistry, visualContext, button, config);
                break;

            case Kx.UI.Elements.ListBox listBox:
                ApplyListBoxProperties(listBox, config);
                break;

            case Kx.UI.Elements.ServerStatus serverStatus:
                ApplyServerStatusProperties(serverStatus, config);
                break;

            case Kx.UI.Elements.TextBox textBox:
                ApplyTextBoxProperties(textBox, config);
                break;

            case Kx.UI.Elements.ProgressBar progressBar:
                ApplyProgressBarProperties(progressBar, config);
                break;

            case StackPanel stackPanel:
                ApplyStackPanelProperties(stackPanel, config);
                break;

            case GridSplitter gridSplitter:
                ApplyGridSplitterProperties(gridSplitter, config);
                break;
        }

        ApplyBindings(control, config);
    }

    private static void ApplyContainerProperties(IControlRegistry registry, IMarkupActionRegistry actionRegistry, IVisualContext context, UIElement control, ControlConfig config) {
        switch (control) {
            case Grid grid:
                ApplyGridDefinitions(grid, config);
                AddChildren(registry, actionRegistry, context, grid, config.Children);
                break;

            case Kx.UI.Elements.Panel.Panel panel:
                AddChildren(registry, actionRegistry, context, panel, config.Children);
                break;
        }
    }

    private static void ApplyBindings(UIElement control, ControlConfig config) {
        BindVisibility(control, config.VisibleBinding);

        switch (control) {
            case Kx.UI.Elements.Label label:
                BindText(label, config.TextBinding);
                BindColor(label, config.ColorBinding);
                BindFontSize(label, config.FontSizeBinding);
                break;

            case Kx.UI.Elements.Button button:
                BindText(button, config.TextBinding);
                BindEnabled(button, config.EnabledBinding);
                BindFontSize(button, config.FontSizeBinding);
                break;

            case Kx.UI.Elements.ListBox listBox:
                BindItems(listBox, GetProperty(config, "itemsBinding"));
                BindSelectedIndex(listBox, GetProperty(config, "selectedIndexBinding"));
                break;

            case Kx.UI.Elements.ServerStatus serverStatus:
                BindServerStatusProperties(serverStatus, config);
                break;

            case Kx.UI.Elements.TextBox textBox:
                BindText(textBox, config.TextBinding);
                break;

            case Kx.UI.Elements.ProgressBar progressBar:
                BindProgress(progressBar, GetProperty(config, "progressBinding"));
                break;

            case StackPanel stackPanel:
                BindOrientation(stackPanel, config.OrientationBinding);
                BindSpacing(stackPanel, config.SpacingBinding);
                break;
        }
    }

    private static void ApplyLabelProperties(Kx.UI.Elements.Label label, ControlConfig config) {
        if (config.Text is not null)
            label.Text.Value = config.Text;

        if (!string.IsNullOrWhiteSpace(config.Color))
            label.Color.Value = SKColor.Parse(config.Color);

        if (config.Font is null)
            return;

        var typeface = CreateTypeface(visualContext: label.Context, config.Font);
        label.Font.Value = new SKFont(typeface, config.Font.Size);
    }

    private static void ApplyButtonProperties(IMarkupActionRegistry actionRegistry, IVisualContext context, Kx.UI.Elements.Button button, ControlConfig config) {
        if (config.Text is not null)
            button.Text = config.Text;

        if (!string.IsNullOrWhiteSpace(config.Color))
            button.ForegroundColor = SKColor.Parse(config.Color);

        if (config.Font is not null) {
            button.FontFamily = config.Font.Name;
            button.FontSize = config.Font.Size;
            button.Bold = config.Font.Style.Contains("Bold", StringComparison.OrdinalIgnoreCase);
            button.Italic = config.Font.Style.Contains("Italic", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(config.Font.Resource))
                button.SetFontTypeface(CreateTypeface(context, config.Font));
        }

        button.SetStateImages(
            ResolveButtonImage(context, config.NormalImage),
            ResolveButtonImage(context, config.HoverImage),
            ResolveButtonImage(context, config.PressedImage));

        if (TryParseAction(config.OnClick, out var actionName, out var argument)) {
            button.Click += () => ExecuteAction(actionRegistry, button, actionName, argument);
        }
    }

    private static void ApplyTextBoxProperties(Kx.UI.Elements.TextBox textBox, ControlConfig config) {
        if (config.Text is not null)
            textBox.Text = config.Text;

        if (!string.IsNullOrWhiteSpace(config.Color))
            textBox.ForegroundColor = SKColor.Parse(config.Color);

        if (config.Font is not null) {
            textBox.FontFamily = config.Font.Name;
            textBox.FontSize = config.Font.Size;
            textBox.Bold = config.Font.Style.Contains("Bold", StringComparison.OrdinalIgnoreCase);
            textBox.Italic = config.Font.Style.Contains("Italic", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(config.Font.Resource))
                textBox.SetFontTypeface(CreateTypeface(textBox.Context, config.Font));
        }

        if (TryGetColorProperty(config, "backgroundColor", out var backgroundColor))
            textBox.BackgroundColor = backgroundColor;

        if (TryGetColorProperty(config, "borderColor", out var borderColor))
            textBox.BorderColor = borderColor;

        if (TryGetColorProperty(config, "scrollBarColor", out var scrollBarColor))
            textBox.ScrollBarColor = scrollBarColor;

        if (TryGetFloatProperty(config, "borderThickness", out var borderThickness))
            textBox.BorderThickness = borderThickness;

        if (TryGetBoolProperty(config, "multiline", out var multiline))
            textBox.Multiline = multiline;

        if (TryGetBoolProperty(config, "readOnly", out var readOnly))
            textBox.ReadOnly = readOnly;

        if (TryGetBoolProperty(config, "glowEnabled", out var glowEnabled))
            textBox.GlowEnabled = glowEnabled;

        if (TryGetColorProperty(config, "glowColor", out var glowColor))
            textBox.GlowColor = glowColor;

        if (TryGetFloatProperty(config, "glowRadius", out var glowRadius))
            textBox.GlowRadius = glowRadius;
    }

    private static void ApplyListBoxProperties(Kx.UI.Elements.ListBox listBox, ControlConfig config) {
        if (!string.IsNullOrWhiteSpace(config.Color))
            listBox.ForegroundColor = SKColor.Parse(config.Color);

        if (config.Font is not null) {
            listBox.FontFamily = config.Font.Name;
            listBox.FontSize = config.Font.Size;
            listBox.Bold = config.Font.Style.Contains("Bold", StringComparison.OrdinalIgnoreCase);
            listBox.Italic = config.Font.Style.Contains("Italic", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(config.Font.Resource))
                listBox.SetFontTypeface(CreateTypeface(listBox.Context, config.Font));
        }

        if (TryGetColorProperty(config, "backgroundColor", out var backgroundColor))
            listBox.BackgroundColor = backgroundColor;

        if (TryGetColorProperty(config, "borderColor", out var borderColor))
            listBox.BorderColor = borderColor;

        if (TryGetColorProperty(config, "scrollBarColor", out var scrollBarColor))
            listBox.ScrollBarColor = scrollBarColor;

        if (TryGetColorProperty(config, "selectedItemColor", out var selectedItemColor))
            listBox.SelectedItemColor = selectedItemColor;

        if (TryGetColorProperty(config, "selectedItemBorderColor", out var selectedItemBorderColor))
            listBox.SelectedItemBorderColor = selectedItemBorderColor;

        if (TryGetColorProperty(config, "hoveredItemColor", out var hoveredItemColor))
            listBox.HoveredItemColor = hoveredItemColor;

        if (TryGetColorProperty(config, "separatorColor", out var separatorColor))
            listBox.SeparatorColor = separatorColor;

        if (TryGetFloatProperty(config, "borderThickness", out var borderThickness))
            listBox.BorderThickness = borderThickness;

        if (TryGetBoolProperty(config, "glowEnabled", out var glowEnabled))
            listBox.GlowEnabled = glowEnabled;

        if (TryGetColorProperty(config, "glowColor", out var glowColor))
            listBox.GlowColor = glowColor;

        if (TryGetFloatProperty(config, "glowRadius", out var glowRadius))
            listBox.GlowRadius = glowRadius;
    }

    private static void ApplyServerStatusProperties(Kx.UI.Elements.ServerStatus serverStatus, ControlConfig config) {
        if (config.Font is not null) {
            serverStatus.FontFamily = config.Font.Name;
            serverStatus.FontSize = config.Font.Size;
            serverStatus.Bold = config.Font.Style.Contains("Bold", StringComparison.OrdinalIgnoreCase);
            serverStatus.Italic = config.Font.Style.Contains("Italic", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(config.Font.Resource))
                serverStatus.SetFontTypeface(CreateTypeface(serverStatus.Context, config.Font));
        }

        if (TryGetBoolProperty(config, "showIndicator", out var showIndicator))
            serverStatus.ShowIndicator = showIndicator;

        if (TryGetBoolProperty(config, "monitoringEnabled", out var monitoringEnabled))
            serverStatus.MonitoringEnabled = monitoringEnabled;

        if (TryGetBoolProperty(config, "showText", out var showText))
            serverStatus.ShowText = showText;

        if (TryGetFloatProperty(config, "indicatorSpacing", out var indicatorSpacing))
            serverStatus.IndicatorSpacing = indicatorSpacing;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "displayName")))
            serverStatus.DisplayName = GetProperty(config, "displayName")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "host")))
            serverStatus.Host = GetProperty(config, "host")!;

        if (TryGetIntProperty(config, "port", out var port))
            serverStatus.Port = port;

        if (TryGetIntProperty(config, "checkIntervalSeconds", out var checkIntervalSeconds))
            serverStatus.CheckIntervalSeconds = checkIntervalSeconds;

        if (TryGetIntProperty(config, "connectTimeoutMilliseconds", out var connectTimeoutMilliseconds))
            serverStatus.ConnectTimeoutMilliseconds = connectTimeoutMilliseconds;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "checkingText")))
            serverStatus.CheckingText = GetProperty(config, "checkingText")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "onlineText")))
            serverStatus.OnlineText = GetProperty(config, "onlineText")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "offlineText")))
            serverStatus.OfflineText = GetProperty(config, "offlineText")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "timeoutText")))
            serverStatus.TimeoutText = GetProperty(config, "timeoutText")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "checkingIndicator")))
            serverStatus.CheckingIndicator = GetProperty(config, "checkingIndicator")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "onlineIndicator")))
            serverStatus.OnlineIndicator = GetProperty(config, "onlineIndicator")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "offlineIndicator")))
            serverStatus.OfflineIndicator = GetProperty(config, "offlineIndicator")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "timeoutIndicator")))
            serverStatus.TimeoutIndicator = GetProperty(config, "timeoutIndicator")!;

        if (!string.IsNullOrWhiteSpace(GetProperty(config, "iconFontFamily")))
            serverStatus.IconFontFamily = GetProperty(config, "iconFontFamily")!;

        if (TryGetFloatProperty(config, "iconFontSize", out var iconFontSize))
            serverStatus.IconFontSize = iconFontSize;

        if (TryGetColorProperty(config, "checkingColor", out var checkingColor))
            serverStatus.CheckingColor = checkingColor;

        if (TryGetColorProperty(config, "onlineColor", out var onlineColor))
            serverStatus.OnlineColor = onlineColor;

        if (TryGetColorProperty(config, "offlineColor", out var offlineColor))
            serverStatus.OfflineColor = offlineColor;

        if (TryGetColorProperty(config, "timeoutColor", out var timeoutColor))
            serverStatus.TimeoutColor = timeoutColor;

        serverStatus.CheckingImage = GetProperty(config, "checkingImage");
        serverStatus.OnlineImage = GetProperty(config, "onlineImage");
        serverStatus.OfflineImage = GetProperty(config, "offlineImage");
        serverStatus.TimeoutImage = GetProperty(config, "timeoutImage");
    }

    private static void ApplyProgressBarProperties(Kx.UI.Elements.ProgressBar progressBar, ControlConfig config) {
        if (TryGetColorProperty(config, "fillColor", out var fillColor))
            progressBar.FillColor = fillColor;

        if (TryGetColorProperty(config, "backgroundColor", out var backgroundColor))
            progressBar.BackgroundColor = backgroundColor;

        if (TryGetColorProperty(config, "borderColor", out var borderColor))
            progressBar.BorderColor = borderColor;

        if (TryGetFloatProperty(config, "borderThickness", out var borderThickness))
            progressBar.BorderThickness = borderThickness;
    }

    private static SKBitmap? ResolveButtonImage(IVisualContext context, string? resourceId) {
        if (string.IsNullOrWhiteSpace(resourceId))
            return null;

        return context is WindowContext windowContext
            ? windowContext.Resources.TryGetSkiaBitmap(resourceId)
            : null;
    }

    private static void ApplyStackPanelProperties(StackPanel stackPanel, ControlConfig config) {
        if (config.Properties.TryGetValue("orientation", out var orientation) &&
            Enum.TryParse<Kx.UI.Layout.Orientation>(orientation, ignoreCase: true, out var parsedOrientation)) {
            stackPanel.Orientation = parsedOrientation;
        }

        if (config.Properties.TryGetValue("spacing", out var spacing) &&
            float.TryParse(spacing, out var parsedSpacing)) {
            stackPanel.Spacing = parsedSpacing;
        }
    }

    private static void ApplyGridSplitterProperties(GridSplitter gridSplitter, ControlConfig config) {
        if (!string.IsNullOrWhiteSpace(config.Color))
            gridSplitter.TrackColor = SKColor.Parse(config.Color);

        if (config.Properties.TryGetValue("orientation", out var orientation) &&
            Enum.TryParse<Kx.UI.Layout.Orientation>(orientation, ignoreCase: true, out var parsedOrientation)) {
            gridSplitter.Orientation = parsedOrientation;
        }

        if (TryGetFloatProperty(config, "minSize", out var minSize))
            gridSplitter.MinSegmentSize = minSize;

        if (TryGetIntProperty(config, "targetColumn", out var targetColumn))
            gridSplitter.TargetColumn = targetColumn;

        if (TryGetIntProperty(config, "targetRow", out var targetRow))
            gridSplitter.TargetRow = targetRow;

        if (TryGetColorProperty(config, "hoverColor", out var hoverColor))
            gridSplitter.HoverTrackColor = hoverColor;

        if (TryGetColorProperty(config, "activeColor", out var activeColor))
            gridSplitter.ActiveTrackColor = activeColor;

        if (TryGetColorProperty(config, "gripColor", out var gripColor))
            gridSplitter.GripColor = gripColor;
    }

    private static SKTypeface CreateTypeface(IVisualContext visualContext, FontConfig font) {
        if (!string.IsNullOrWhiteSpace(font.Resource) && visualContext is WindowContext windowContext) {
            SKTypeface? resourceTypeface = windowContext.Resources.TryGetSkiaTypeface(font.Resource);
            if (resourceTypeface is not null)
                return resourceTypeface;
        }

        var weight = font.Style.Contains("Bold", StringComparison.OrdinalIgnoreCase)
            ? SKFontStyleWeight.Bold
            : SKFontStyleWeight.Normal;

        var slant = font.Style.Contains("Italic", StringComparison.OrdinalIgnoreCase)
            ? SKFontStyleSlant.Italic
            : SKFontStyleSlant.Upright;

        return SKTypeface.FromFamilyName(font.Name, weight, SKFontStyleWidth.Normal, slant);
    }

    private static void ApplyGridDefinitions(Grid grid, ControlConfig config) {
        grid.Rows.Clear();
        foreach (var row in config.Rows)
            grid.Rows.Add(new RowDefinition { Height = ToGridLength(row.Height) });

        grid.Columns.Clear();
        foreach (var column in config.Columns)
            grid.Columns.Add(new ColumnDefinition { Width = ToGridLength(column.Width) });
    }

    private static void AddChildren(IControlRegistry registry, IMarkupActionRegistry actionRegistry, IVisualContext context, Kx.UI.Elements.Panel.Panel panel, IEnumerable<ControlConfig> children) {
        foreach (var childConfig in children) {
            var child = Create(registry, actionRegistry, context, childConfig);
            panel.AddChild(child);
        }
    }

    private static void ExecuteAction(IMarkupActionRegistry actionRegistry, UIElement source, string actionName, string? argument) {
        var context = new Kx.UI.Actions.MarkupActionContext(source.Context, source, actionName, argument);
        if (!actionRegistry.TryExecute(context))
            throw new InvalidOperationException($"No markup action has been registered for '{actionName}'.");
    }

    private static void BindText(Kx.UI.Elements.Label label, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(label, path, value => {
            if (UiStateValueConverter.TryGetText(value, out var text))
                label.Text.Value = text;
        });
    }

    private static void BindItems(Kx.UI.Elements.ListBox listBox, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(listBox, path, value => {
            switch (value) {
                case IEnumerable<string> items:
                    listBox.SetItems(items);
                    break;

                case IEnumerable<object?> rawItems:
                    listBox.SetItems(rawItems.Select(item => item?.ToString() ?? string.Empty));
                    break;

                case string item:
                    listBox.SetItems([item]);
                    break;

                default:
                    listBox.SetItems([]);
                    break;
            }
        });
    }

    private static void BindSelectedIndex(Kx.UI.Elements.ListBox listBox, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!UiBindingExpression.TryParse(path, out var binding) || binding is null)
            throw new InvalidOperationException($"The binding expression '{path}' is invalid.");

        ApplyStateValue(listBox.Context, binding, value => {
            if (UiStateValueConverter.TryGetInt(value, out var selectedIndex))
                listBox.SetSelectedIndex(selectedIndex, notify: false);
        });

        listBox.TrackDisposable(listBox.Context.State.Subscribe(binding.Path, value => ApplyStateValue(listBox.Context, binding, value, convertedValue => {
            if (UiStateValueConverter.TryGetInt(convertedValue, out var selectedIndex))
                listBox.SetSelectedIndex(selectedIndex, notify: false);
        })));

        listBox.SelectedIndexChanged += (selectedIndex, _) => listBox.Context.State.Set(binding.Path, selectedIndex);
    }

    private static void BindText(Kx.UI.Elements.TextBox textBox, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(textBox, path, value => {
            if (UiStateValueConverter.TryGetText(value, out var text))
                textBox.Text = text;
        });
    }

    private static void BindServerStatusProperties(Kx.UI.Elements.ServerStatus serverStatus, ControlConfig config) {
        BindServerStatusBool(serverStatus, GetProperty(config, "monitoringEnabledBinding"), value => serverStatus.MonitoringEnabled = value);
        BindServerStatusText(serverStatus, GetProperty(config, "displayNameBinding"), value => serverStatus.DisplayName = value);
        BindServerStatusText(serverStatus, GetProperty(config, "hostBinding"), value => serverStatus.Host = value);
        BindServerStatusInt(serverStatus, GetProperty(config, "portBinding"), value => serverStatus.Port = value);
        BindServerStatusInt(serverStatus, GetProperty(config, "checkIntervalBinding"), value => serverStatus.CheckIntervalSeconds = value);
        BindServerStatusInt(serverStatus, GetProperty(config, "connectTimeoutBinding"), value => serverStatus.ConnectTimeoutMilliseconds = value);
        BindServerStatusText(serverStatus, GetProperty(config, "checkingTextBinding"), value => serverStatus.CheckingText = value);
        BindServerStatusText(serverStatus, GetProperty(config, "onlineTextBinding"), value => serverStatus.OnlineText = value);
        BindServerStatusText(serverStatus, GetProperty(config, "offlineTextBinding"), value => serverStatus.OfflineText = value);
        BindServerStatusText(serverStatus, GetProperty(config, "timeoutTextBinding"), value => serverStatus.TimeoutText = value);
    }

    private static void BindProgress(Kx.UI.Elements.ProgressBar progressBar, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(progressBar, path, value => {
            if (!UiStateValueConverter.TryGetFloat(value, out var progress))
                return;

            progressBar.Progress = progress > 1f ? progress / 100f : progress;
        });
    }

    private static void BindFontSize(Kx.UI.Elements.Label label, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(label, path, value => {
            if (!UiStateValueConverter.TryGetFloat(value, out var fontSize))
                return;

            label.Font.Value = new SKFont(label.Font.Value.Typeface, fontSize);
        });
    }

    private static void BindFontSize(Kx.UI.Elements.Button button, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(button, path, value => {
            if (!UiStateValueConverter.TryGetFloat(value, out var fontSize))
                return;

            button.FontSize = fontSize;
            button.Context.RequestRender();
        });
    }

    private static void BindText(Kx.UI.Elements.Button button, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(button, path, value => {
            if (UiStateValueConverter.TryGetText(value, out var text)) {
                button.Text = text;
                button.Context.RequestRender();
            }
        });
    }

    private static void BindOrientation(StackPanel stackPanel, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(stackPanel, path, value => {
            if (!UiStateValueConverter.TryGetOrientation(value, out var orientation))
                return;

            stackPanel.Orientation = orientation;
            stackPanel.Context.RequestRender();
        });
    }

    private static void BindSpacing(StackPanel stackPanel, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(stackPanel, path, value => {
            if (!UiStateValueConverter.TryGetFloat(value, out var spacing))
                return;

            stackPanel.Spacing = spacing;
            stackPanel.Context.RequestRender();
        });
    }

    private static void BindColor(Kx.UI.Elements.Label label, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(label, path, value => {
            if (UiStateValueConverter.TryGetColor(value, out var color))
                label.Color.Value = color;
        });
    }

    private static void BindServerStatusText(Kx.UI.Elements.ServerStatus serverStatus, string? path, Action<string> apply) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(serverStatus, path, value => {
            if (UiStateValueConverter.TryGetText(value, out var text))
                apply(text);
        });
    }

    private static void BindServerStatusInt(Kx.UI.Elements.ServerStatus serverStatus, string? path, Action<int> apply) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(serverStatus, path, value => {
            if (UiStateValueConverter.TryGetInt(value, out var numericValue))
                apply(numericValue);
        });
    }

    private static void BindServerStatusBool(Kx.UI.Elements.ServerStatus serverStatus, string? path, Action<bool> apply) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(serverStatus, path, value => {
            if (UiStateValueConverter.TryGetBool(value, out var boolValue))
                apply(boolValue);
        });
    }

    private static void BindVisibility(UIElement control, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(control, path, value => {
            if (UiStateValueConverter.TryGetBool(value, out var visible))
                control.Visible = visible;
        });
    }

    private static void BindEnabled(Kx.UI.Elements.Button button, string? path) {
        if (string.IsNullOrWhiteSpace(path))
            return;

        BindState(button, path, value => {
            if (UiStateValueConverter.TryGetBool(value, out var enabled)) {
                button.IsEnabled = enabled;
                button.Context.RequestRender();
            }
        });
    }

    private static void BindState(UIElement control, string expression, Action<object?> apply) {
        if (!UiBindingExpression.TryParse(expression, out var binding) || binding is null)
            throw new InvalidOperationException($"The binding expression '{expression}' is invalid.");

        ApplyStateValue(control.Context, binding, apply);
        control.TrackDisposable(control.Context.State.Subscribe(binding.Path, value => ApplyStateValue(control.Context, binding, value, apply)));
    }

    private static void ApplyStateValue(IVisualContext context, UiBindingExpression binding, Action<object?> apply) {
        if (context.State.TryGet(binding.Path, out var value))
            ApplyStateValue(context, binding, value, apply);
    }

    private static void ApplyStateValue(IVisualContext context, UiBindingExpression binding, object? value, Action<object?> apply) {
        if (!UiStateValueConverter.TryApplyBindingConverters(value, binding.Converters, out var convertedValue))
            return;

        if (context.UiThread.InvokeRequired)
            context.UiThread.BeginInvoke(new Action(() => apply(convertedValue)));
        else
            apply(convertedValue);
    }

    private static bool TryParseAction(string? actionExpression, out string actionName, out string? argument) {
        actionName = string.Empty;
        argument = null;

        if (string.IsNullOrWhiteSpace(actionExpression))
            return false;

        int separatorIndex = actionExpression.IndexOf(':');
        if (separatorIndex < 0) {
            actionName = actionExpression.Trim();
            return true;
        }

        actionName = actionExpression[..separatorIndex].Trim();
        argument = actionExpression[(separatorIndex + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(actionName);
    }

    private static string? GetProperty(ControlConfig config, string key) {
        return config.Properties.TryGetValue(key, out var value)
            ? value
            : null;
    }

    private static bool TryGetBoolProperty(ControlConfig config, string key, out bool value) {
        return bool.TryParse(GetProperty(config, key), out value);
    }

    private static bool TryGetFloatProperty(ControlConfig config, string key, out float value) {
        return float.TryParse(GetProperty(config, key), out value);
    }

    private static bool TryGetIntProperty(ControlConfig config, string key, out int value) {
        return int.TryParse(GetProperty(config, key), out value);
    }

    private static bool TryGetColorProperty(ControlConfig config, string key, out SKColor color) {
        var value = GetProperty(config, key);
        if (string.IsNullOrWhiteSpace(value)) {
            color = default;
            return false;
        }

        color = SKColor.Parse(value);
        return true;
    }

    private static Thickness ToThickness(ThicknessConfig config) {
        return new Thickness(config.Left, config.Top, config.Right, config.Bottom);
    }

    private static GridLength ToGridLength(GridLengthConfig config) {
        return config.Unit.ToUpperInvariant() switch {
            "AUTO" => GridLength.Auto,
            "PIXEL" => GridLength.Pixel(config.Value),
            _ => GridLength.Star(config.Value <= 0 ? 1 : config.Value)
        };
    }
}
