// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.Events;
using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Elements;
using Kx.Core.Extensions;

using SkiaSharp;

namespace Kx.UI.Elements;

public class Button : UIElement {
    public string Text { get; set; }
    public float FontSize { get; set; } = 14f;
    public bool IsEnabled { get; set; } = true;
    public override bool CanFocus => IsEnabled;

    public event Action? Click;

    private bool _isPressed;
    private bool _isHovered;

    private readonly SKPaint _textPaint = new() { IsAntialias = true, Color = new SKColor(0, 0, 0, 255) };
    private readonly SKPaint _bgPaint = new() { IsAntialias = true, Color = new SKColor(230, 230, 230, 255) };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Color = new SKColor(180, 180, 180, 255), StrokeWidth = 1, Style = SKPaintStyle.Stroke };

    private SKFont? _font;
    private float _scaledFontSize;

    public Button(IVisualContext ctx, string id, string text) : base(ctx, id) {
        Text = text;
        _scaledFontSize = FontSize * DpiScale;
        _font = new SKFont(SKTypeface.Default, _scaledFontSize);
    }

    // Visual exposes OnDpiChanged(float) as virtual — override to react to DPI changes
    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
        _scaledFontSize = FontSize * scale;
        _font = new SKFont(SKTypeface.Default, _scaledFontSize);
        _borderPaint.StrokeWidth = Math.Max(1f, scale);
        Invalidate();
    }

    public override void Measure(float dpi) {
        _scaledFontSize = FontSize * dpi;
        _font = new SKFont(SKTypeface.Default, _scaledFontSize);

        var text = Text ?? string.Empty;
        _font.MeasureText(text, out SKRect textBounds);

        var padH = (int)Math.Ceiling((Padding.Left + Padding.Right) * dpi);
        var padV = (int)Math.Ceiling((Padding.Top + Padding.Bottom) * dpi);

        int width = (int)Math.Ceiling(textBounds.Width) + padH;
        int height = (int)Math.Ceiling(textBounds.Height) + padV;

        width = Math.Max(width, (int)Math.Ceiling(48 * dpi));
        height = Math.Max(height, (int)Math.Ceiling(28 * dpi));

        DesiredSize = new Size(width, height).AddMargin(Margin, dpi);
    }

    public override void Arrange(Rectangle finalRect, float dpi) {
        finalRect = finalRect.ApplyMargin(Margin, dpi);

        LayoutRect = finalRect;
        _bounds.Value = finalRect;
    }

    // UIElement verlangt protected abstract OnDraw(SKCanvas)
    protected override void OnDraw(SKCanvas canvas) {
        if (!Visible)
            return;

        if (!IsEnabled) {
            _bgPaint.Color = new SKColor(240, 240, 240, 255);
            _textPaint.Color = new SKColor(160, 160, 160, 255);
        } else if (_isPressed) {
            _bgPaint.Color = new SKColor(200, 200, 200, 255);
            _textPaint.Color = new SKColor(0, 0, 0, 255);
        } else if (_isHovered || IsFocused) {
            _bgPaint.Color = new SKColor(220, 220, 220, 255);
            _textPaint.Color = new SKColor(0, 0, 0, 255);
        } else {
            _bgPaint.Color = new SKColor(245, 245, 245, 255);
            _textPaint.Color = new SKColor(0, 0, 0, 255);
        }

        var r = LayoutRect;
        var skRect = new SKRect(r.Left, r.Top, r.Right, r.Bottom);

        canvas.DrawRect(skRect, _bgPaint);
        canvas.DrawRect(skRect, _borderPaint);

        var text = Text ?? string.Empty;
        _font ??= new SKFont(SKTypeface.Default, _scaledFontSize);

        _font.MeasureText(text, out SKRect textBounds);

        float x = r.Left + (r.Width - textBounds.Width) / 2f - textBounds.Left;
        float y = r.Top + (r.Height - textBounds.Height) / 2f - textBounds.Top;

        canvas.DrawText(text, x, y, _font, _textPaint);
    }

    // Input Handling

    public override bool OnMouseDown(Point p) {
        if (!IsEnabled)
            return false;
        if (!Bounds.Contains(p))
            return false;

        _isPressed = true;
        Invalidate();
        return true;
    }

    public override bool OnMouseUp(Point p) {
        if (!IsEnabled)
            return false;

        var wasPressed = _isPressed;
        _isPressed = false;

        if (wasPressed && Bounds.Contains(p)) {
            Click?.Invoke();
            return true;
        }

        return false;
    }

    public override bool OnMouseMove(Point p) {
        if (!IsEnabled)
            return false;

        var hovered = Bounds.Contains(p);
        if (hovered != _isHovered) {
            _isHovered = hovered;
            Invalidate();
        }

        return _isHovered;
    }

    public override bool OnKeyDown(KeyCode key) {
        if (!IsEnabled)
            return false;

        if (IsFocused && (key == KeyCode.Enter || key == KeyCode.Space)) {
            _isPressed = true;
            Invalidate();
            return true;
        }

        return false;
    }

    public override bool OnKeyUp(KeyCode key) {
        if (!IsEnabled)
            return false;

        if (IsFocused && (key == KeyCode.Enter || key == KeyCode.Space)) {
            if (_isPressed) {
                _isPressed = false;
                Click?.Invoke();
                Invalidate();
                return true;
            }
        }

        return false;
    }

    public override void OnFocusGained() => Invalidate();

    public override void OnFocusLost() {
        _isPressed = false;
        _isHovered = false;
        Invalidate();
    }
}
