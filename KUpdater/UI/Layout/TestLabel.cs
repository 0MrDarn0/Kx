// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace KUpdater.UI.Layout;

public class TestLabel : UIElement {
    private readonly string _text;
    private readonly SKPaint _paint;
    private readonly SKFont _font;

    public TestLabel(WindowContext ctx, string id, string text, float size)
        : base(ctx, id, () => Rectangle.Empty) {
        _text = text;
        _paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        _font = new SKFont(SKTypeface.Default, size);
    }

    public override void Measure(float dpi) {
        var width = _font.MeasureText(_text);
        var height = _font.Metrics.Descent - _font.Metrics.Ascent;

        DesiredSize = new Size((int)width, (int)height);
    }

    public override void Arrange(Rectangle rect, float dpi) {
        LayoutRect = rect;
    }
    protected override void OnDraw(SKCanvas canvas) {

    }
    public override void Draw(SKCanvas canvas) {
        base.Draw(canvas);

        using var rectPaint = new SKPaint {
            Color = new SKColor(255, 255, 255, 180),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };

        canvas.DrawRect(LayoutRect.ToSKRect(), rectPaint);
        canvas.DrawText(
            _text,
            LayoutRect.X,
            LayoutRect.Y - _font.Metrics.Ascent,
            _font,
            _paint
        );
    }


}
