// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.Layout;
using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.VisualTree;
using Kx.UI.Elements.Panel;
using Kx.UI.Layout;

using SkiaSharp;

namespace Kx.UI.Markup;

public static class ControlFactory {
    public static UIElement Create(IControlRegistry registry, IVisualContext context, ControlConfig config) {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(config);

        if (!registry.TryCreate(context, config, out var control) || control is null)
            throw new InvalidOperationException($"No control factory has been registered for type '{config.Type}'.");

        ApplyCommonProperties(control, config);
        ApplyContainerProperties(registry, context, control, config);
        return control;
    }

    private static void ApplyCommonProperties(UIElement control, ControlConfig config) {
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
                ApplyButtonProperties(button, config);
                break;

            case StackPanel stackPanel:
                ApplyStackPanelProperties(stackPanel, config);
                break;
        }
    }

    private static void ApplyContainerProperties(IControlRegistry registry, IVisualContext context, UIElement control, ControlConfig config) {
        switch (control) {
            case Grid grid:
                ApplyGridDefinitions(grid, config);
                AddChildren(registry, context, grid, config.Children);
                break;

            case Kx.UI.Elements.Panel.Panel panel:
                AddChildren(registry, context, panel, config.Children);
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

    private static void ApplyButtonProperties(Kx.UI.Elements.Button button, ControlConfig config) {
        if (config.Text is not null)
            button.Text = config.Text;

        if (config.Font is not null)
            button.FontSize = config.Font.Size;

        if (string.Equals(config.OnClick, "closeWindow", StringComparison.OrdinalIgnoreCase))
            button.Click += button.Context.CloseWindow;
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

    private static void AddChildren(IControlRegistry registry, IVisualContext context, Kx.UI.Elements.Panel.Panel panel, IEnumerable<ControlConfig> children) {
        foreach (var childConfig in children) {
            var child = Create(registry, context, childConfig);
            panel.AddChild(child);
        }
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
