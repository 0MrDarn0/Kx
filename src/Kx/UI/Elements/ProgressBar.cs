// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;

using SkiaSharp;

namespace Kx.UI.Elements;

public sealed class ProgressBar(IVisualContext context, string id) : UIElement(context, id) {
    private KxColor _fillColor = SKColors.Goldenrod.ToKxColor();
    private KxColor _backgroundColor = SKColors.Transparent.ToKxColor();
    private KxColor _borderColor = SKColors.Black.ToKxColor();
    private float _progress;
    private float _borderThickness = 1f;

    public float Progress {
        get => _progress;
        set {
            _progress = Math.Clamp(value, 0f, 1f);
            Invalidate();
        }
    }

    public KxColor FillColor {
        get => _fillColor;
        set {
            _fillColor = value;
            Invalidate();
        }
    }

    public KxColor BorderColor {
        get => _borderColor;
        set {
            _borderColor = value;
            Invalidate();
        }
    }

    public KxColor BackgroundColor {
        get => _backgroundColor;
        set {
            _backgroundColor = value;
            Invalidate();
        }
    }

    public float BorderThickness {
        get => _borderThickness;
        set {
            _borderThickness = Math.Max(0f, value);
            Invalidate();
        }
    }

    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
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
        if (!Visible)
            return;

        Rectangle rect = LayoutRect;
        canvas.DrawRect(rect.Left, rect.Top, rect.Right, rect.Bottom, _backgroundColor);

        if (Progress > 0f)
            canvas.DrawRect(rect.Left, rect.Top, rect.Left + (rect.Width * Progress), rect.Bottom, _fillColor);

        if (BorderThickness > 0f)
            canvas.DrawRectStroke(rect.Left, rect.Top, rect.Right, rect.Bottom, _borderColor, _borderThickness * DpiScale);
    }
}
