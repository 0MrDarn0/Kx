// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Core.Extensions;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;

using SkiaSharp;

namespace Kx.UI.Elements;

public sealed class ProgressBar : UIElement {
    private readonly SKPaint _fillPaint = new() { IsAntialias = true, Color = SKColors.Goldenrod };
    private readonly SKPaint _backgroundPaint = new() { IsAntialias = true, Color = SKColors.Transparent };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
    private float _progress;
    private float _borderThickness = 1f;

    public ProgressBar(IVisualContext context, string id) : base(context, id) {
    }

    public float Progress {
        get => _progress;
        set {
            _progress = Math.Clamp(value, 0f, 1f);
            Invalidate();
        }
    }

    public KxColor FillColor {
        get => _fillPaint.Color.ToKxColor();
        set {
            _fillPaint.Color = value.ToSKColor();
            Invalidate();
        }
    }

    public KxColor BorderColor {
        get => _borderPaint.Color.ToKxColor();
        set {
            _borderPaint.Color = value.ToSKColor();
            Invalidate();
        }
    }

    public KxColor BackgroundColor {
        get => _backgroundPaint.Color.ToKxColor();
        set {
            _backgroundPaint.Color = value.ToSKColor();
            Invalidate();
        }
    }

    public float BorderThickness {
        get => _borderThickness;
        set {
            _borderThickness = Math.Max(0f, value);
            _borderPaint.StrokeWidth = _borderThickness * DpiScale;
            Invalidate();
        }
    }

    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
        _borderPaint.StrokeWidth = _borderThickness * scale;
    }

    public override void Measure(float dpi) {
        if (FixedBounds is Rectangle fixedBounds) {
            DesiredSize = new Size(
                fixedBounds.Width + (int)(Margin.Horizontal * dpi),
                fixedBounds.Height + (int)(Margin.Vertical * dpi));
            return;
        }

        DesiredSize = new Size((int)(240 * dpi), (int)(8 * dpi));
    }

    protected override void OnDraw(IKxCanvas canvas) {
        var skCanvas = canvas.As<SKCanvas>();
        if (skCanvas is null)
            return;

        if (!Visible)
            return;

        Rectangle rect = LayoutRect;
        skCanvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _backgroundPaint);

        if (Progress > 0f)
            skCanvas.DrawRect(rect.Left, rect.Top, rect.Width * Progress, rect.Height, _fillPaint);

        if (BorderThickness > 0f)
            skCanvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _borderPaint);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _fillPaint.Dispose();
            _backgroundPaint.Dispose();
            _borderPaint.Dispose();
        }

        base.Dispose(disposing);
    }
}
