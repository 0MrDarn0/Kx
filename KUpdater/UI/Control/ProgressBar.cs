// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Extensions;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.UI.Control;

[ExposeToLua]
public class ProgressBar : IControl {
    public string Id { get; }
    private readonly Func<Rectangle> _boundsFunc;
    public Rectangle Bounds => _boundsFunc();

    public bool Visible { get; set; } = true;
    private float _progress = 0f;
    public float Progress {
        get => _progress;
        set => _progress = Math.Clamp(value, 0f, 1f);
    }

    // Farben
    private SKColor _fillColor = SKColors.Goldenrod;
    private SKColor _borderColor = SKColors.Black;
    private SKColor _backgroundColor = SKColors.Transparent;

    public SKColor FillColor {
        get => _fillColor;
        set { _fillColor = value; _fillPaint.Color = value; }
    }
    public SKColor BorderColor {
        get => _borderColor;
        set { _borderColor = value; _borderPaint.Color = value; }
    }
    public SKColor BackgroundColor {
        get => _backgroundColor;
        set { _backgroundColor = value; _bgPaint.Color = value; }
    }

    private bool _disposed;

    // 🧩 Skia Paints cachen
    private readonly SKPaint _fillPaint;
    private readonly SKPaint _borderPaint;
    private readonly SKPaint _bgPaint;

    // 🧩 Font/Text Ressourcen
    private SKTypeface? _typeface;
    private SKFont? _skFont;
    private SKPaint? _skTextPaint;
    private readonly bool _ownsFont;

    public Font Font { get; set; }
    public Color TextColor { get; set; } = Color.Black;

    public ProgressBar(string id, Func<Rectangle> boundsFunc, Font font, Color textColor, bool ownsFont = true) {
        Id = id;
        _boundsFunc = boundsFunc;
        Font = font;
        TextColor = textColor;
        _ownsFont = ownsFont;

        _fillPaint = new SKPaint { Color = _fillColor, IsAntialias = true };
        _borderPaint = new SKPaint { Color = _borderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
        _bgPaint = new SKPaint { Color = _backgroundColor, IsAntialias = true };

        InitTextResources();
    }

    public ProgressBar(string id, Table bounds, Font font, Color textColor, bool ownsFont = true)
        : this(id, () => new Rectangle(
            (int)(bounds.Get("x").CastToNumber() ?? 0),
            (int)(bounds.Get("y").CastToNumber() ?? 0),
            (int)(bounds.Get("width").CastToNumber() ?? 0),
            (int)(bounds.Get("height").CastToNumber() ?? 0)
        ), font, textColor, ownsFont) {
    }

    private void InitTextResources() {
        SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        _typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
        _skFont = new SKFont(_typeface, Font.Size * 1.33f);
        _skTextPaint = new SKPaint { Color = TextColor.ToSKColor(), IsAntialias = true };
    }
    public void Draw(Graphics g) {
        if (!Visible)
            return;
        var rect = Bounds;

        using var brush = new SolidBrush(Color.FromArgb(FillColor.Alpha, FillColor.Red, FillColor.Green, FillColor.Blue));
        g.FillRectangle(brush, rect.X, rect.Y, rect.Width * Progress, rect.Height);
        g.DrawRectangle(Pens.White, rect);
    }

    public void Draw(SKCanvas canvas) {
        if (!Visible)
            return;
        var rect = Bounds;

        // Hintergrund
        if (_backgroundColor.Alpha > 0)
            canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _bgPaint);

        // Fortschritt
        float barWidth = rect.Width * Progress;
        canvas.DrawRect(rect.X, rect.Y, barWidth, rect.Height, _fillPaint);

        // Rahmen
        canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, _borderPaint);

        // Prozent-Text mittig
        string percentText = $"{(int)(Progress * 100)}%";
        var metrics = _skFont!.Metrics;
        float x = rect.X + rect.Width / 2;
        float y = rect.Y + rect.Height / 2 - (metrics.Ascent + metrics.Descent) / 2 - metrics.Descent * 0.3f;

        canvas.DrawText(percentText, x, y, SKTextAlign.Center, _skFont, _skTextPaint!);
    }

    public bool OnMouseMove(Point p) => false;
    public bool OnMouseDown(Point p) => false;
    public bool OnMouseUp(Point p) => false;
    public bool OnMouseWheel(int delta, Point p) => false;

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            _fillPaint.Dispose();
            _borderPaint.Dispose();
            _bgPaint.Dispose();

            if (_ownsFont)
                Font.Dispose();

            _typeface?.Dispose();
            _skFont?.Dispose();
            _skTextPaint?.Dispose();
        }

        _disposed = true;
    }
}
