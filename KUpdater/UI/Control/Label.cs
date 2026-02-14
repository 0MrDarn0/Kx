// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Core.Extensions;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.UI.Control;

[ExposeToLua]
public class Label : ControlBase {
    private readonly Property<string> _text;
    public string Text {
        get => _text.Value;
        set => _text.Value = value;
    }

    private readonly Property<Font> _font;
    public Font Font {
        get => _font.Value;
        set => _font.Value = value;
    }

    private readonly Property<Color> _color;
    public Color Color {
        get => _color.Value;
        set => _color.Value = value;
    }

    public TextFormatFlags Flags { get; set; }
    private readonly bool _ownsFont;
    private bool _disposed;

    // 🧩 Skia-Caches
    private SKTypeface? _typeface;
    private SKFont? _skFont;
    private SKPaint? _skPaint;

    public Label(
        string id,
        Func<Rectangle> boundsFunc,
        string text,
        Font font,
        Color color,
        bool ownsFont = true,
        TextFormatFlags flags = TextFormatFlags.Default)
        : base(UIContextProvider.Current ?? throw new InvalidOperationException("UI context not initialized"), id, boundsFunc) {
        Flags = flags;
        _ownsFont = ownsFont;

        // Properties marshal to UI thread and request render on change
        _text = new Property<string>(_ui, text, () => Invalidate());
        _font = new Property<Font>(_ui, font ?? throw new ArgumentNullException(nameof(font)), () => { InitResources(); Invalidate(); });
        _color = new Property<Color>(_ui, color, () => { UpdatePaintColor(); Invalidate(); });

        InitResources();
    }

    public Label(
        string id,
        Table bounds,
        string text,
        Font font,
        Color color,
        bool ownsFont = true,
        TextFormatFlags flags = TextFormatFlags.Default)
        : this(id, bounds.ToBoundsFunc(), text, font, color, ownsFont, flags) {
    }

    private void InitResources() {
        // Dispose any previous resources if reinitializing
        try { _skPaint?.Dispose(); }
        catch { }
        try { _skFont?.Dispose(); }
        catch { }
        try { _typeface?.Dispose(); }
        catch { }

        var font = Font;
        var color = Color;

        SKFontStyleWeight weight = font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        _typeface = SKTypeface.FromFamilyName(font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
        _skFont = new SKFont(_typeface, font.Size * 1.33f);
        _skPaint = new SKPaint {
            Color = color.ToSKColor(),
            IsAntialias = true
        };
    }

    private void UpdatePaintColor() {
        if (_skPaint == null)
            _skPaint = new SKPaint { IsAntialias = true };
        _skPaint.Color = Color.ToSKColor();
    }

    public override void Draw(SKCanvas canvas) {
        if (!Visible || _skFont == null || _skPaint == null)
            return;

        var bounds = Bounds;
        var metrics = _skFont.Metrics;

        var x = bounds.X;
        var y = bounds.Y + bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;

        canvas.DrawText(Text, x, y, SKTextAlign.Left, _skFont, _skPaint);
    }

    public override bool OnMouseMove(Point p) => false;
    public override bool OnMouseDown(Point p) => false;
    public override bool OnMouseUp(Point p) => false;
    public override bool OnMouseWheel(int delta, Point p) => false;

    protected override void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            if (_ownsFont) {
                try { Font.Dispose(); }
                catch { }
            }

            try { _skPaint?.Dispose(); }
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
