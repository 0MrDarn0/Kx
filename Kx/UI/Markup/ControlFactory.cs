// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Actions;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.Layout;
using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.State;
using Kx.Abstractions.UI.VisualTree;
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

        ApplyCommonProperties(actionRegistry, control, config);
        ApplyContainerProperties(registry, actionRegistry, context, control, config);
        return control;
    }

    private static void ApplyCommonProperties(IMarkupActionRegistry actionRegistry, UIElement control, ControlConfig config) {
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

        control.GridRow = config.GridRow;
        control.GridColumn = config.GridColumn;
        control.GridRowSpan = Math.Max(1, config.GridRowSpan);
        control.GridColumnSpan = Math.Max(1, config.GridColumnSpan);

        switch (control) {
            case Kx.UI.Elements.Label label:
                ApplyLabelProperties(label, config);
                break;

            case Kx.UI.Elements.Button button:
                ApplyButtonProperties(actionRegistry, button, config);
                break;

            case StackPanel stackPanel:
                ApplyStackPanelProperties(stackPanel, config);
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

        var typeface = CreateTypeface(config.Font);
        label.Font.Value = new SKFont(typeface, config.Font.Size);
    }

    private static void ApplyButtonProperties(IMarkupActionRegistry actionRegistry, Kx.UI.Elements.Button button, ControlConfig config) {
        if (config.Text is not null)
            button.Text = config.Text;

        if (config.Font is not null)
            button.FontSize = config.Font.Size;

        if (TryParseAction(config.OnClick, out var actionName, out var argument)) {
            button.Click += () => ExecuteAction(actionRegistry, button, actionName, argument);
        }
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

    private static SKTypeface CreateTypeface(FontConfig font) {
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

    private static void BindState(UIElement control, string path, Action<object?> apply) {
        ApplyStateValue(control.Context, path, apply);
        control.TrackDisposable(control.Context.State.Subscribe(path, value => ApplyStateValue(control.Context, value, apply)));
    }

    private static void ApplyStateValue(IVisualContext context, string path, Action<object?> apply) {
        if (context.State.TryGet(path, out var value))
            ApplyStateValue(context, value, apply);
    }

    private static void ApplyStateValue(IVisualContext context, object? value, Action<object?> apply) {
        if (context.UiThread.InvokeRequired)
            context.UiThread.BeginInvoke(new Action(() => apply(value)));
        else
            apply(value);
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
