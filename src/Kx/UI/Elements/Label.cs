// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Binding;
using Kx.Sdk.UI.Elements;
using Kx.Core.Extensions;

using SkiaSharp;

namespace Kx.UI.Elements;

public class Label : UIElement {
    public Property<string> Text { get; }
    public Property<SKFont> Font { get; }
    public Property<SKColor> Color { get; }

    public Label(IVisualContext ctx, string id, string text, float size) : base(ctx, id) {
        Text = new Property<string>(ctx.UiThread, text, Invalidate);
        Font = new Property<SKFont>(ctx.UiThread, new SKFont(SKTypeface.Default, size), Invalidate);
        Color = new Property<SKColor>(ctx.UiThread, SKColors.White, Invalidate);
    }

    /// <summary>
    /// Assigns the foreground color and returns the same label for fluent configuration.
    /// </summary>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same label instance.</returns>
    public Label WithForeground(SKColor color) {
        Color.Value = color;
        return this;
    }

    public override void Measure(float dpi) {
        var width = Font.Value.MeasureText(Text.Value);
        var height = Font.Value.Metrics.Descent - Font.Value.Metrics.Ascent;

        DesiredSize = new Size((int)width, (int)height)
            .AddMargin(Margin, dpi)
            .AddPadding(Padding, dpi);
    }

    protected override void OnDraw(SKCanvas canvas) {
        using var paint = new SKPaint {
            Color = Color.Value,
            IsAntialias = true
        };

        canvas.DrawText(
            Text.Value,
            ContentRect.X,
            ContentRect.Y - Font.Value.Metrics.Ascent,
            Font.Value,
            paint
        );
    }
}
