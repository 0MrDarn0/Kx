// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Events;
using Kx.Sdk.Rendering;
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
    private KxColor _foregroundColor = new(0, 0, 0, 255);
    private KxColor _backgroundColor = new(245, 245, 245, 255);
    private KxColor _hoverBackgroundColor = new(220, 220, 220, 255);
    private KxColor _pressedBackgroundColor = new(200, 200, 200, 255);
    private KxColor _disabledBackgroundColor = new(240, 240, 240, 255);
    private KxColor _disabledForegroundColor = new(160, 160, 160, 255);
    private KxColor _borderColor = new(180, 180, 180, 255);
    private float _borderThickness = 1f;

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

    public KxColor ForegroundColor {
        get => _foregroundColor;
        set {
            _foregroundColor = value;
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

    public KxColor HoverBackgroundColor {
        get => _hoverBackgroundColor;
        set {
            _hoverBackgroundColor = value;
            Invalidate();
        }
    }

    public KxColor PressedBackgroundColor {
        get => _pressedBackgroundColor;
        set {
            _pressedBackgroundColor = value;
            Invalidate();
        }
    }

    public KxColor DisabledBackgroundColor {
        get => _disabledBackgroundColor;
        set {
            _disabledBackgroundColor = value;
            Invalidate();
        }
    }

    public KxColor DisabledForegroundColor {
        get => _disabledForegroundColor;
        set {
            _disabledForegroundColor = value;
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

    public float BorderThickness {
        get => _borderThickness;
        set {
            _borderThickness = Math.Max(0f, value);
            Invalidate();
        }
    }

    public event Action? Click;

    /// <summary>
    /// Assigns the foreground color and returns the same button for fluent configuration.
    /// </summary>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same button instance.</returns>
    public Button WithForeground(KxColor color) {
        ForegroundColor = color;
        return this;
    }

    /// <summary>
    /// Assigns the disabled foreground color and returns the same button for fluent configuration.
    /// </summary>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same button instance.</returns>
    public Button WithDisabledForeground(KxColor color) {
        DisabledForegroundColor = color;
        return this;
    }

    /// <summary>
    /// Assigns border color and thickness and returns the same button for fluent configuration.
    /// </summary>
    /// <param name="color">The border color to apply.</param>
    /// <param name="thickness">The border thickness to apply.</param>
    /// <returns>The same button instance.</returns>
    public Button WithBorder(KxColor color, float thickness) {
        BorderColor = color;
        BorderThickness = thickness;
        return this;
    }

    /// <summary>
    /// Assigns button background colors for all interaction states and returns the same button.
    /// </summary>
    /// <param name="normal">The default background color.</param>
    /// <param name="hover">The hover background color.</param>
    /// <param name="pressed">The pressed background color.</param>
    /// <param name="disabled">The disabled background color.</param>
    /// <returns>The same button instance.</returns>
    public Button WithButtonStates(KxColor normal, KxColor hover, KxColor pressed, KxColor disabled) {
        BackgroundColor = normal;
        HoverBackgroundColor = hover;
        PressedBackgroundColor = pressed;
        DisabledBackgroundColor = disabled;
        return this;
    }

    /// <summary>
    /// Attaches a click handler and returns the same button for fluent configuration.
    /// </summary>
    /// <param name="onClick">The callback invoked for click events.</param>
    /// <returns>The same button instance.</returns>
    public Button OnClick(Action onClick) {
        ArgumentNullException.ThrowIfNull(onClick);
        Click += onClick;
        return this;
    }

    private bool _isPressed;
    private bool _isHovered;

    private SKFont? _font;
    private SKTypeface? _typeface;
    private SKTypeface? _customTypeface;
    private float _scaledFontSize;
    private SKBitmap? _normalImage;
    private SKBitmap? _hoverImage;
    private SKBitmap? _pressedImage;

    public Button(IVisualContext ctx, string id, string text) : base(ctx, id) {
        _text = text;
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
        _borderThickness = Math.Max(1f, scale);
        Invalidate();
    }

    public override void Measure(float dpi) {
        UpdateFont();

        var text = Text ?? string.Empty;
        _font ??= new SKFont(SKTypeface.Default, _scaledFontSize);
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
    protected override void OnDraw(IKxCanvas canvas) {
        if (!Visible)
            return;

        var r = LayoutRect;

        var stateImage = _isPressed
            ? _pressedImage ?? _hoverImage ?? _normalImage
            : (_isHovered || IsFocused)
                ? _hoverImage ?? _normalImage
                : _normalImage;

        KxColor textColor;

        if (stateImage is not null) {
            canvas.DrawBitmap(stateImage, LayoutRect.Left, LayoutRect.Top, LayoutRect.Right, LayoutRect.Bottom);
            textColor = IsEnabled
                ? _foregroundColor
                : _disabledForegroundColor;
        } else {
            KxColor backgroundColor;

            if (!IsEnabled) {
                backgroundColor = _disabledBackgroundColor;
                textColor = _disabledForegroundColor;
            } else if (_isPressed) {
                backgroundColor = _pressedBackgroundColor;
                textColor = _foregroundColor;
            } else if (_isHovered || IsFocused) {
                backgroundColor = _hoverBackgroundColor;
                textColor = _foregroundColor;
            } else {
                backgroundColor = _backgroundColor;
                textColor = _foregroundColor;
            }

            canvas.DrawRect(r.Left, r.Top, r.Right, r.Bottom, backgroundColor);
            canvas.DrawRectStroke(r.Left, r.Top, r.Right, r.Bottom, _borderColor, _borderThickness);
        }

        var text = Text ?? string.Empty;
        _font ??= new SKFont(SKTypeface.Default, _scaledFontSize);

        _font.MeasureText(text, out SKRect textBounds);

        float x = r.Left + (r.Width - textBounds.Width) / 2f - textBounds.Left;
        float y = r.Top + (r.Height - textBounds.Height) / 2f - textBounds.Top;

        canvas.DrawText(text, x, y, _scaledFontSize, textColor, _fontFamily, _bold, _italic);
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
