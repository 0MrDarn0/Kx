// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Events;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;

using SkiaSharp;

namespace Kx.UI.Elements;

public class Button : UIElement {
    private string _text;
    private string _fontFamily = "Segoe UI";
    private float _fontSize = 14f;
    private bool _bold;
    private bool _italic;
    private bool _isEnabled = true;
    private SKColor _foregroundColor = new(0, 0, 0, 255);
    private SKColor _backgroundColor = new(245, 245, 245, 255);
    private SKColor _hoverBackgroundColor = new(220, 220, 220, 255);
    private SKColor _pressedBackgroundColor = new(200, 200, 200, 255);
    private SKColor _disabledBackgroundColor = new(240, 240, 240, 255);
    private SKColor _disabledForegroundColor = new(160, 160, 160, 255);
    private SKColor _borderColor = new(180, 180, 180, 255);

    public string Text {
        get => _text;
        set {
            _text = value ?? string.Empty;
            Invalidate();
        }
    }

    public string FontFamily {
        get => _fontFamily;
        set {
            _fontFamily = string.IsNullOrWhiteSpace(value) ? "Segoe UI" : value;
            UpdateFont();
            Invalidate();
        }
    }

    public float FontSize {
        get => _fontSize;
        set {
            float normalizedValue = value > 0 ? value : 14f;
            if (Math.Abs(normalizedValue - _fontSize) < 0.001f)
                return;

            _fontSize = normalizedValue;
            UpdateFont();
            Invalidate();
        }
    }

    public bool Bold {
        get => _bold;
        set {
            _bold = value;
            UpdateFont();
            Invalidate();
        }
    }

    public bool Italic {
        get => _italic;
        set {
            _italic = value;
            UpdateFont();
            Invalidate();
        }
    }

    public bool IsEnabled {
        get => _isEnabled;
        set {
            if (_isEnabled == value)
                return;

            _isEnabled = value;
            Invalidate();
        }
    }

    public override bool CanFocus => IsEnabled;

    public SKColor ForegroundColor {
        get => _foregroundColor;
        set {
            _foregroundColor = value;
            Invalidate();
        }
    }

    public SKColor BackgroundColor {
        get => _backgroundColor;
        set {
            _backgroundColor = value;
            Invalidate();
        }
    }

    public SKColor HoverBackgroundColor {
        get => _hoverBackgroundColor;
        set {
            _hoverBackgroundColor = value;
            Invalidate();
        }
    }

    public SKColor PressedBackgroundColor {
        get => _pressedBackgroundColor;
        set {
            _pressedBackgroundColor = value;
            Invalidate();
        }
    }

    public SKColor DisabledBackgroundColor {
        get => _disabledBackgroundColor;
        set {
            _disabledBackgroundColor = value;
            Invalidate();
        }
    }

    public SKColor DisabledForegroundColor {
        get => _disabledForegroundColor;
        set {
            _disabledForegroundColor = value;
            Invalidate();
        }
    }

    public SKColor BorderColor {
        get => _borderColor;
        set {
            _borderColor = value;
            _borderPaint.Color = value;
            Invalidate();
        }
    }

    public float BorderThickness {
        get => _borderPaint.StrokeWidth;
        set {
            _borderPaint.StrokeWidth = Math.Max(0f, value);
            Invalidate();
        }
    }

    public event Action? Click;

    private bool _isPressed;
    private bool _isHovered;

    private readonly SKPaint _textPaint = new() { IsAntialias = true, Color = new SKColor(0, 0, 0, 255) };
    private readonly SKPaint _bgPaint = new() { IsAntialias = true, Color = new SKColor(230, 230, 230, 255) };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Color = new SKColor(180, 180, 180, 255), StrokeWidth = 1, Style = SKPaintStyle.Stroke };

    private SKFont? _font;
    private SKTypeface? _typeface;
    private SKTypeface? _customTypeface;
    private float _scaledFontSize;
    private SKBitmap? _normalImage;
    private SKBitmap? _hoverImage;
    private SKBitmap? _pressedImage;

    public Button(IVisualContext ctx, string id, string text) : base(ctx, id) {
        _text = text;
        _borderPaint.Color = _borderColor;
        UpdateFont();
    }

    /// <summary>
    /// Sets an explicit typeface that overrides family-name lookup for this button.
    /// </summary>
    public void SetFontTypeface(SKTypeface? typeface) {
        _customTypeface?.Dispose();
        _customTypeface = typeface;
        UpdateFont();
        Invalidate();
    }

    /// <summary>
    /// Configures optional button state images for the normal, hover, and pressed visual states.
    /// </summary>
    public void SetStateImages(SKBitmap? normalImage, SKBitmap? hoverImage, SKBitmap? pressedImage) {
        _normalImage?.Dispose();
        _hoverImage?.Dispose();
        _pressedImage?.Dispose();

        _normalImage = normalImage;
        _hoverImage = hoverImage;
        _pressedImage = pressedImage;
        Invalidate();
    }

    // Visual exposes OnDpiChanged(float) as virtual — override to react to DPI changes
    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
        UpdateFont();
        _borderPaint.StrokeWidth = Math.Max(1f, scale);
        Invalidate();
    }

    public override void Measure(float dpi) {
        UpdateFont();

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
        base.Arrange(finalRect, dpi);
    }

    // UIElement verlangt protected abstract OnDraw(SKCanvas)
    protected override void OnDraw(SKCanvas canvas) {
        if (!Visible)
            return;

        var r = LayoutRect;

        var stateImage = _isPressed
            ? _pressedImage ?? _hoverImage ?? _normalImage
            : (_isHovered || IsFocused)
                ? _hoverImage ?? _normalImage
                : _normalImage;

        if (stateImage is not null) {
            canvas.DrawBitmap(stateImage, new SKRect(LayoutRect.Left, LayoutRect.Top, LayoutRect.Right, LayoutRect.Bottom));
            _textPaint.Color = IsEnabled
                ? ForegroundColor
                : DisabledForegroundColor;
        } else {
            if (!IsEnabled) {
                _bgPaint.Color = DisabledBackgroundColor;
                _textPaint.Color = DisabledForegroundColor;
            } else if (_isPressed) {
                _bgPaint.Color = PressedBackgroundColor;
                _textPaint.Color = ForegroundColor;
            } else if (_isHovered || IsFocused) {
                _bgPaint.Color = HoverBackgroundColor;
                _textPaint.Color = ForegroundColor;
            } else {
                _bgPaint.Color = BackgroundColor;
                _textPaint.Color = ForegroundColor;
            }

            _borderPaint.Color = BorderColor;

            var skRect = new SKRect(r.Left, r.Top, r.Right, r.Bottom);

            canvas.DrawRect(skRect, _bgPaint);
            canvas.DrawRect(skRect, _borderPaint);
        }

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

        if (wasPressed)
            Invalidate();

        if (wasPressed && Bounds.Contains(p)) {
            Click?.Invoke();
            return true;
        }

        return wasPressed;
    }

    public override bool OnMouseMove(Point p) {
        if (!IsEnabled)
            return false;

        var hovered = Bounds.Contains(p);
        if (hovered != _isHovered) {
            _isHovered = hovered;
            Invalidate();
            return true;
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

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _normalImage?.Dispose();
            _hoverImage?.Dispose();
            _pressedImage?.Dispose();
            _font?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateFont() {
        _scaledFontSize = FontSize * DpiScale;

        _font?.Dispose();

        if (_customTypeface is null) {
            _typeface?.Dispose();

            var weight = _bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            var slant = _italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            _typeface = SKTypeface.FromFamilyName(_fontFamily, weight, SKFontStyleWidth.Normal, slant);
        } else {
            _typeface?.Dispose();
            _typeface = null;
        }

        _font = new SKFont(_customTypeface ?? _typeface ?? SKTypeface.Default, _scaledFontSize);
    }
}
