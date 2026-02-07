// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;
using KUpdater.Extensions;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.UI.Control;

[ExposeToLua]
public class Label : IControl {
    public string Id { get; }
    private readonly Func<Rectangle> _boundsFunc;
    public Rectangle Bounds => _boundsFunc();
    public string Text { get; set; }
    public Font Font { get; private set; }
    public Color Color { get; set; }
    public TextFormatFlags Flags { get; set; }
    public bool Visible { get; set; } = true;
    private readonly bool _ownsFont;
    private bool _disposed;

    // 🧩 Skia-Caches
    private SKTypeface? _typeface;
    private SKFont? _skFont;
    private SKPaint? _skPaint;

    public Label(string id, Func<Rectangle> boundsFunc, string text, Font font, Color color, bool ownsFont = true, TextFormatFlags flags = TextFormatFlags.Default) {
        Id = id;
        _boundsFunc = boundsFunc;
        Text = text;
        Font = font;
        Color = color;
        Flags = flags;
        _ownsFont = ownsFont;

        InitResources();
    }

    public Label(string id, Table bounds, string text, Font font, Color color,
                   bool ownsFont = true, TextFormatFlags flags = TextFormatFlags.Default)
        : this(id, () => new Rectangle(
            (int)(bounds.Get("x").CastToNumber() ?? 0),
            (int)(bounds.Get("y").CastToNumber() ?? 0),
            (int)(bounds.Get("width").CastToNumber() ?? 0),
            (int)(bounds.Get("height").CastToNumber() ?? 0)
        ), text, font, color, ownsFont, flags) {
    }


    private void InitResources() {
        SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        _typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
        _skFont = new SKFont(_typeface, Font.Size * 1.33f);
        _skPaint = new SKPaint {
            Color = Color.ToSKColor(),
            IsAntialias = true
        };
    }

    public void Draw(SKCanvas canvas) {
        if (!Visible || _skFont == null || _skPaint == null)
            return;

        var bounds = Bounds;
        var metrics = _skFont.Metrics;

        var x = bounds.X;
        var y = bounds.Y + bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2;

        canvas.DrawText(Text, x, y, SKTextAlign.Left, _skFont, _skPaint);
    }

    public bool OnMouseMove(Point p) => false;
    public bool OnMouseDown(Point p) => false;
    public bool OnMouseUp(Point p) => false;
    public bool OnMouseWheel(int delta, Point p) => false;

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this); // verhindert unnötigen Finalizer
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            // Managed Ressourcen freigeben
            if (_ownsFont)
                Font.Dispose();

            _skPaint?.Dispose();
            _skFont?.Dispose();
            _typeface?.Dispose();
        }

        // Unmanaged Ressourcen hier freigeben
        _disposed = true;
    }
}
