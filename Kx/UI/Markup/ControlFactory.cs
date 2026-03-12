// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Actions;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.Layout;
using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.VisualTree;
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
