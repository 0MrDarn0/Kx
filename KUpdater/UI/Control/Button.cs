// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Extensions;
using KUpdater.Utility;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.UI.Control;

public class Button : ControlBase {
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

    private readonly Property<string> _skinKey;
    public string SkinKey {
        get => _skinKey.Value;
        set => _skinKey.Value = value;
    }

    // OnClick kann aus Lua gesetzt werden; marshallen wir ebenfalls auf UI-Thread
    private readonly Property<Action?> _onClick;
    public Action? OnClick {
        get => _onClick.Value;
        set => _onClick.Value = value;
    }

    public bool IsHovered { get; private set; }
    public bool IsPressed { get; private set; }

    private readonly bool _ownsFont;
    private bool _disposed;

    private readonly IResourceProvider? _resourceProvider;

    // Cache für Bilder
    private readonly Dictionary<string, SKBitmap> _stateBitmaps = [];

    // paint,font and Typeface cachen
    private SKTypeface? _typeface;
    private SKFont? _skFont;
    private SKPaint? _skPaint;

    public Button(
        string id,
        Func<Rectangle> boundsFunc,
        string text,
        Font font,
        Color color,
        string skinKey,
        Action? onClick,
        IResourceProvider? resourceProvider = null,
        bool ownsFont = true)
        : base(UIContextProvider.Current ?? throw new InvalidOperationException("UI context not initialized"), id, boundsFunc) {
        _ownsFont = ownsFont;
        _resourceProvider = resourceProvider;

        // Property values: onChanged triggers a render for visual properties
        _text = new Property<string>(_ui, text, () => Invalidate());
        _font = new Property<Font>(_ui, font, () => { InitResources(); Invalidate(); });
        _color = new Property<Color>(_ui, color, () => { UpdatePaintColor(); Invalidate(); });
        _skinKey = new Property<string>(_ui, skinKey, () => { LoadStateBitmaps(); Invalidate(); });

        // OnClick does not need to trigger a render; keep null onChanged
        _onClick = new Property<Action?>(_ui, onClick, null);

        InitResources();
    }

    public Button(
        string id,
        Table bounds,
        string text,
        Font font,
        Color color,
        string themeKey,
        Action? onClick,
        IResourceProvider? resourceProvider = null,
        bool ownsFont = true)
        : this(id, bounds.ToBoundsFunc(), text, font, color, themeKey, onClick, resourceProvider, ownsFont) {
    }

    private void InitResources() {
        // Dispose previous resources if reinitializing
        try { _skPaint?.Dispose(); }
        catch { }
        try { _skFont?.Dispose(); }
        catch { }
        try { _typeface?.Dispose(); }
        catch { }

        // Load bitmaps for current skin key
        LoadStateBitmaps();

        SKFontStyleWeight weight = Font.Style.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = Font.Style.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        _typeface = SKTypeface.FromFamilyName(Font.Name, new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));
        _skFont = new SKFont(_typeface, Font.Size * 1.33f);
        _skPaint = new SKPaint { Color = Color.ToSKColor(), IsAntialias = true };
    }

    private void UpdatePaintColor() {
        if (_skPaint == null)
            _skPaint = new SKPaint { IsAntialias = true };

        _skPaint.Color = Color.ToSKColor();
    }

    private void LoadStateBitmaps() {
        // Clear existing bitmaps
        foreach (var bmp in _stateBitmaps.Values) {
            try { bmp.Dispose(); }
            catch { }
        }
        _stateBitmaps.Clear();

        var provider = _resourceProvider ?? _ctx.Resources;
        try {
            provider?.LoadControlStateResources(SkinKey, Id, _stateBitmaps);
        }
        catch {
            // swallow resource load errors; renderer will draw Fallback
        }
    }

    public override void Draw(SKCanvas canvas) {
        if (!Visible)
            return;

        string state = IsPressed ? "click" : IsHovered ? "hover" : "normal";
        if (_stateBitmaps.TryGetValue(state, out var img) && img != null) {
            var bounds = Bounds;
            var destRect = new SKRect(bounds.X, bounds.Y, bounds.Right, bounds.Bottom);
            canvas.DrawBitmap(img, destRect);
        }

        if (_skFont == null || _skPaint == null)
            return;

        var metrics = _skFont.Metrics;
        var x = Bounds.X + Bounds.Width / 2;
        var y = Bounds.Y + Bounds.Height / 2 - (metrics.Ascent + metrics.Descent) / 2 - metrics.Descent * 0.3f;

        canvas.DrawText(Text, x, y, SKTextAlign.Center, _skFont, _skPaint);
    }

    public override bool OnMouseMove(Point p) {
        bool prev = IsHovered;
        IsHovered = Bounds.Contains(p);
        if (prev != IsHovered)
            Invalidate();
        return prev != IsHovered;
    }

    public override bool OnMouseDown(Point p) {
        bool prev = IsPressed;
        IsPressed = Bounds.Contains(p);
        if (prev != IsPressed)
            Invalidate();
        return prev != IsPressed;
    }

    public override bool OnMouseUp(Point p) {
        bool prevPressed = IsPressed;
        if (IsPressed && Bounds.Contains(p)) {
            // Invoke OnClick on UI thread (Property ensures marshaling)
            var handler = OnClick;
            try { handler?.Invoke(); }
            catch { }
        }
        IsPressed = false;
        if (prevPressed != IsPressed)
            Invalidate();
        return prevPressed != IsPressed;
    }

    public override bool OnMouseWheel(int delta, Point p) => false;

    protected override void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            if (_ownsFont) {
                try { Font.Dispose(); }
                catch { }
            }

            foreach (var bmp in _stateBitmaps.Values) {
                try { bmp.Dispose(); }
                catch { }
            }
            _stateBitmaps.Clear();

            try { _typeface?.Dispose(); }
            catch { }
            try { _skPaint?.Dispose(); }
            catch { }
            try { _skFont?.Dispose(); }
            catch { }
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}
