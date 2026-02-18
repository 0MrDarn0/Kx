// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Extensions;
using KUpdater.UI.Binding;
using KUpdater.UI.Manager;
using KUpdater.Utility;
using SkiaSharp;

namespace KUpdater.UI.Control;

public class ProgressBar : ControlBase {
    // progress value (0..1)
    private readonly Property<float> _progress;
    public float Progress {
        get => _progress.Value;
        set => _progress.Value = Math.Clamp(value, 0f, 1f);
    }

    // Skia paints (cached)
    private SKPaint _fillPaint;
    private SKPaint _borderPaint;
    private SKPaint _bgPaint;

    // Colors are Property so Lua can set them from any thread
    private readonly Property<SKColor> _fillColor;
    private readonly Property<SKColor> _borderColor;
    private readonly Property<SKColor> _backgroundColor;

    // Text resources
    private SKTypeface? _typeface;
    private SKFont? _skFont;
    private SKPaint? _skTextPaint;
    private readonly bool _ownsFont;

    // Public font and text color (TextColor is System.Drawing.Color)
    private readonly Property<Font> _font;
    private readonly Property<Color> _textColor;

    public Font Font {
        get => _font.Value;
        set => _font.Value = value;
    }

    public Color TextColor {
        get => _textColor.Value;
        set => _textColor.Value = value;
    }

    // Backing for convenience properties (not Property)
    public SKColor FillColor {
        get => _fillColor.Value;
        set => _fillColor.Value = value;
    }

    public SKColor BorderColor {
        get => _borderColor.Value;
        set => _borderColor.Value = value;
    }

    public SKColor BackgroundColor {
        get => _backgroundColor.Value;
        set => _backgroundColor.Value = value;
    }

    private bool _disposed;

    public ProgressBar(
        string id,
        Func<Rectangle> boundsFunc,
        Font font,
        Color textColor,
        Color fillColor,
        Color borderColor,
        Color backgroundColor,
        bool ownsFont = true)
        : base(UIContextProvider.Current ?? throw new InvalidOperationException("UI context not initialized"), id, boundsFunc) {
        _ownsFont = ownsFont;

        // Initialize paints with constructor colors
        _fillPaint = new SKPaint { Color = fillColor.ToSKColor(), IsAntialias = true };
        _borderPaint = new SKPaint { Color = borderColor.ToSKColor(), Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        _bgPaint = new SKPaint { Color = backgroundColor.ToSKColor(), IsAntialias = true };

        // Property fields: onChanged callbacks update paints/text resources and request render
        _progress = new Property<float>(_ui, 0f, () => Invalidate());

        // Use the constructor colors as the initial values for the properties
        _fillColor = new Property<SKColor>(_ui, fillColor.ToSKColor(), () => { _fillPaint.Color = _fillColor!.Value; Invalidate(); });
        _borderColor = new Property<SKColor>(_ui, borderColor.ToSKColor(), () => { _borderPaint.Color = _borderColor!.Value; Invalidate(); });
        _backgroundColor = new Property<SKColor>(_ui, backgroundColor.ToSKColor(), () => { _bgPaint.Color = _backgroundColor!.Value; Invalidate(); });

        _font = new Property<Font>(_ui, font, () => { InitTextResources(); Invalidate(); });
        _textColor = new Property<Color>(_ui, textColor, () => { UpdateTextPaintColor(); Invalidate(); });

        // Initialize text resources
        InitTextResources();
    }

    private void InitTextResources() {
        // Dispose previous text resources safely
        try { _skTextPaint?.Dispose(); }
        catch { }
        try { _skFont?.Dispose(); }
        catch { }
        try { _typeface?.Dispose(); }
        catch { }

        var font = Font ?? throw new ArgumentNullException(nameof(Font));
        SKFontStyleWeight weight = font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        _typeface = SKTypeface.FromFamilyName(font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
        _skFont = new SKFont(_typeface, font.Size * 1.33f);
        _skTextPaint = new SKPaint { Color = TextColor.ToSKColor(), IsAntialias = true };
    }

    private void UpdateTextPaintColor() {
        if (_skTextPaint == null)
            _skTextPaint = new SKPaint { IsAntialias = true };
        _skTextPaint.Color = TextColor.ToSKColor();
    }


    public override void Draw(SKCanvas canvas) {
        if (!Visible)
            return;

        var rect = Bounds;

        // Background
        if (_bgPaint.Color.Alpha > 0)
            canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _bgPaint);

        // Progress bar fill
        float clamped = Math.Clamp(Progress, 0f, 1f);
        float barWidth = rect.Width * clamped;
        if (barWidth > 0)
            canvas.DrawRect(rect.X, rect.Y, barWidth, rect.Height, _fillPaint);

        // Border
        canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _borderPaint);

        // Percent text centered with Glow
        if (_skFont != null && _skTextPaint != null) {
            string percentText = $"{(int)(clamped * 100)}%";
            var metrics = _skFont.Metrics;

            float x = rect.X + rect.Width / 2f;
            float y = rect.Y + rect.Height / 2f - (metrics.Ascent + metrics.Descent) / 2f;
            var skRect = new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
            InkGasm.DrenchGlowOut(canvas, percentText, x, y - 18, _skFont, _skTextPaint);

        }
    }

    public override bool OnMouseMove(Point p) => false;
    public override bool OnMouseDown(Point p) => false;
    public override bool OnMouseUp(Point p) => false;
    public override bool OnMouseWheel(int delta, Point p) => false;

    protected override void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            try { _fillPaint?.Dispose(); }
            catch { }
            try { _borderPaint?.Dispose(); }
            catch { }
            try { _bgPaint?.Dispose(); }
            catch { }

            if (_ownsFont) {
                try { Font?.Dispose(); }
                catch { }
            }

            try { _skTextPaint?.Dispose(); }
            catch { }
            try { _skFont?.Dispose(); }
            catch { }
            try { _typeface?.Dispose(); }
            catch { }
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}
