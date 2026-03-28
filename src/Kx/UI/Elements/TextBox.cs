// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.Events;

using SkiaSharp;

namespace Kx.UI.Elements;

public sealed class TextBox : UIElement {
    private const float DefaultFontSize = 14f;
    private const int ScrollBarWidth = 8;
    private const int MinimumScrollMarkerHeight = 20;

    private readonly SKPaint _textPaint = new() { IsAntialias = true, Color = SKColors.White };
    private readonly SKPaint _backgroundPaint = new() { IsAntialias = true, Color = new SKColor(16, 16, 16) };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Color = SKColors.Gold, Style = SKPaintStyle.Stroke, StrokeWidth = 2f };
    private readonly SKPaint _scrollBarPaint = new() { IsAntialias = true, Color = new SKColor(124, 110, 75, 160) };
    private readonly SKPaint _glowPaint = new() { IsAntialias = true, Color = new SKColor(255, 255, 255, 180), Style = SKPaintStyle.Stroke, StrokeWidth = 2f };

    private string _text;
    private string _fontFamily = "Segoe UI";
    private float _fontSize = DefaultFontSize;
    private bool _bold;
    private bool _italic;
    private SKTypeface? _typeface;
    private SKFont? _font;
    private int _scrollOffset;
    private bool _isDraggingScrollMarker;
    private int _dragStartY;
    private int _scrollOffsetAtDragStart;
    private SKRect _scrollMarkerRect;

    public TextBox(IVisualContext context, string id, string text) : base(context, id) {
        _text = text;
        Padding = new Kx.Sdk.UI.Layout.Thickness(6);
        UpdateFont();
    }

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
            _fontSize = value > 0 ? value : DefaultFontSize;
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

    public SKColor ForegroundColor {
        get => _textPaint.Color;
        set {
            _textPaint.Color = value;
            Invalidate();
        }
    }

    public SKColor BackgroundColor {
        get => _backgroundPaint.Color;
        set {
            _backgroundPaint.Color = value;
            Invalidate();
        }
    }

    public SKColor BorderColor {
        get => _borderPaint.Color;
        set {
            _borderPaint.Color = value;
            Invalidate();
        }
    }

    public float BorderThickness {
        get => _borderPaint.StrokeWidth;
        set {
            float thickness = Math.Max(0f, value);
            _borderPaint.StrokeWidth = thickness;
            _glowPaint.StrokeWidth = thickness;
            Invalidate();
        }
    }

    public SKColor ScrollBarColor {
        get => _scrollBarPaint.Color;
        set {
            _scrollBarPaint.Color = value;
            Invalidate();
        }
    }

    public bool Multiline { get; set; } = true;
    public bool ReadOnly { get; set; } = true;
    public override bool CanFocus => true;
    public bool GlowEnabled { get; set; }

    public SKColor GlowColor {
        get => _glowPaint.Color;
        set {
            _glowPaint.Color = value;
            Invalidate();
        }
    }

    public float GlowRadius { get; set; } = 6f;

    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
        UpdateFont();
    }

    public override void Measure(float dpi) {
        if (FixedBounds is Rectangle fixedBounds) {
            DesiredSize = new Size(
                fixedBounds.Width + (int)(Margin.Horizontal * dpi),
                fixedBounds.Height + (int)(Margin.Vertical * dpi));
            return;
        }

        DesiredSize = new Size((int)(320 * dpi), (int)(180 * dpi));
    }

    protected override void OnDraw(SKCanvas canvas) {
        if (_font is null || !Visible)
            return;

        Rectangle rect = LayoutRect;
        Rectangle contentRect = ContentRect;

        canvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _backgroundPaint);

        if (GlowEnabled && BorderThickness > 0f) {
            using var glowImageFilter = SKImageFilter.CreateBlur(GlowRadius, GlowRadius);
            _glowPaint.ImageFilter = glowImageFilter;
            canvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _glowPaint);
            _glowPaint.ImageFilter = null;
        }

        if (BorderThickness > 0f)
            canvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _borderPaint);

        float availableTextWidth = Math.Max(8f, contentRect.Width - ScrollBarWidth - 4f);
        List<string> wrappedLines = GetWrappedLines(availableTextWidth);
        int lineHeight = GetLineHeight();
        int totalTextHeight = wrappedLines.Count * lineHeight;
        int maxScroll = Math.Max(0, totalTextHeight - contentRect.Height);
        ClampScrollOffset(maxScroll);

        canvas.Save();
        canvas.ClipRect(new SKRect(contentRect.Left, contentRect.Top, contentRect.Right - ScrollBarWidth, contentRect.Bottom));

        float baseline = contentRect.Top - _font.Metrics.Ascent - _scrollOffset;
        foreach (string line in wrappedLines) {
            canvas.DrawText(line, contentRect.Left, baseline, _font, _textPaint);
            baseline += lineHeight;
        }

        canvas.Restore();

        DrawScrollBar(canvas, contentRect, totalTextHeight, maxScroll);
    }

    public override bool OnMouseDown(Point point) {
        if (!Bounds.Contains(point))
            return false;

        if (_scrollMarkerRect.Contains(point.X, point.Y)) {
            _isDraggingScrollMarker = true;
            _dragStartY = point.Y;
            _scrollOffsetAtDragStart = _scrollOffset;
            return true;
        }

        if (point.X >= Bounds.Right - ScrollBarWidth) {
            JumpToScrollPosition(point.Y);
            return true;
        }

        // Click inside content area -> request focus so keyboard input is routed here
        try {
            Context.UIElementManager.SetFocus(this);
        }
        catch {
            // best-effort, don't throw during input handling
        }

        return true;
    }

    public override bool OnKeyDown(KeyCode key) {
        if (!IsFocused || ReadOnly)
            return false;

        // Basic editing behavior: backspace, enter, space, tab and simple char keys
        try {
            switch (key) {
                case KeyCode.Backspace:
                    if (!string.IsNullOrEmpty(_text)) {
                        _text = _text.Substring(0, Math.Max(0, _text.Length - 1));
                        Invalidate();
                        return true;
                    }
                    return false;
                case KeyCode.Enter:
                    if (Multiline) {
                        _text += "\n";
                        Invalidate();
                        return true;
                    }
                    return false;
                case KeyCode.Space:
                    _text += ' ';
                    Invalidate();
                    return true;
                case KeyCode.Tab:
                    _text += '\t';
                    Invalidate();
                    return true;
                default:
                    // Try to append single-character key names (A, B, C, 0..9)
                    var name = key.ToString();
                    if (!string.IsNullOrEmpty(name) && name.Length == 1) {
                        // append lowercase
                        _text += char.ToLowerInvariant(name[0]);
                        Invalidate();
                        return true;
                    }
                    break;
            }
        }
        catch {
            // ignore errors in best-effort input handling
        }

        return false;
    }

    public override bool OnMouseMove(Point point) {
        if (!_isDraggingScrollMarker)
            return false;

        Rectangle contentRect = ContentRect;
        float availableTextWidth = Math.Max(8f, contentRect.Width - ScrollBarWidth - 4f);
        int totalTextHeight = GetWrappedLines(availableTextWidth).Count * GetLineHeight();
        int maxScroll = Math.Max(0, totalTextHeight - contentRect.Height);
        if (maxScroll <= 0)
            return false;

        float markerHeight = Math.Max(MinimumScrollMarkerHeight, contentRect.Height * (contentRect.Height / (float)totalTextHeight));
        float markerTravel = Math.Max(1f, contentRect.Height - markerHeight);
        float delta = point.Y - _dragStartY;
        float scrollRatio = delta / markerTravel;

        _scrollOffset = _scrollOffsetAtDragStart + (int)(scrollRatio * maxScroll);
        ClampScrollOffset(maxScroll);
        return true;
    }

    public override bool OnMouseUp(Point point) {
        bool wasDraggingScrollMarker = _isDraggingScrollMarker;
        _isDraggingScrollMarker = false;
        return wasDraggingScrollMarker;
    }

    public override bool OnMouseWheel(int delta, Point point) {
        if (!Bounds.Contains(point))
            return false;

        Rectangle contentRect = ContentRect;
        float availableTextWidth = Math.Max(8f, contentRect.Width - ScrollBarWidth - 4f);
        int totalTextHeight = GetWrappedLines(availableTextWidth).Count * GetLineHeight();
        int maxScroll = Math.Max(0, totalTextHeight - contentRect.Height);
        if (maxScroll <= 0)
            return false;

        _scrollOffset -= Math.Sign(delta) * GetLineHeight() * 3;
        ClampScrollOffset(maxScroll);
        return true;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _font?.Dispose();
            _typeface?.Dispose();
            _textPaint.Dispose();
            _backgroundPaint.Dispose();
            _borderPaint.Dispose();
            _scrollBarPaint.Dispose();
            _glowPaint.Dispose();
        }

        base.Dispose(disposing);
    }

    private void DrawScrollBar(SKCanvas canvas, Rectangle contentRect, int totalTextHeight, int maxScroll) {
        _scrollMarkerRect = SKRect.Empty;
        if (maxScroll <= 0)
            return;

        float visibleHeight = contentRect.Height;
        float markerHeight = Math.Max(MinimumScrollMarkerHeight, visibleHeight * (visibleHeight / totalTextHeight));
        float markerY = contentRect.Top + (_scrollOffset / (float)maxScroll) * (visibleHeight - markerHeight);

        _scrollMarkerRect = new SKRect(contentRect.Right - ScrollBarWidth + 2, markerY, contentRect.Right - 2, markerY + markerHeight);
        canvas.DrawRect(_scrollMarkerRect, _scrollBarPaint);
    }

    private void JumpToScrollPosition(int mouseY) {
        Rectangle contentRect = ContentRect;
        float availableTextWidth = Math.Max(8f, contentRect.Width - ScrollBarWidth - 4f);
        int totalTextHeight = GetWrappedLines(availableTextWidth).Count * GetLineHeight();
        int maxScroll = Math.Max(0, totalTextHeight - contentRect.Height);
        if (maxScroll <= 0)
            return;

        float clickRatio = Math.Clamp((mouseY - contentRect.Top) / (float)Math.Max(1, contentRect.Height), 0f, 1f);
        _scrollOffset = (int)(clickRatio * maxScroll);
        ClampScrollOffset(maxScroll);
    }

    private void ClampScrollOffset(int maxScroll) {
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
    }

    private int GetLineHeight() {
        if (_font is null)
            return (int)Math.Ceiling(FontSize * 1.2f);

        SKFontMetrics metrics = _font.Metrics;
        return Math.Max(1, (int)Math.Ceiling((metrics.Descent - metrics.Ascent) * 1.2f));
    }

    private List<string> GetWrappedLines(float availableWidth) {
        List<string> lines = [];
        if (_font is null)
            return lines;

        string[] paragraphs = Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        foreach (string paragraph in paragraphs) {
            if (!Multiline) {
                lines.Add(paragraph);
                continue;
            }

            if (string.IsNullOrEmpty(paragraph)) {
                lines.Add(string.Empty);
                continue;
            }

            string currentLine = string.Empty;
            foreach (string word in paragraph.Split(' ', StringSplitOptions.None)) {
                string candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                if (string.IsNullOrEmpty(currentLine) || _font.MeasureText(candidate) <= availableWidth) {
                    currentLine = candidate;
                    continue;
                }

                lines.Add(currentLine);
                currentLine = word;
            }

            lines.Add(currentLine);
        }

        if (lines.Count == 0)
            lines.Add(string.Empty);

        return lines;
    }

    private void UpdateFont() {
        _font?.Dispose();
        _typeface?.Dispose();

        SKFontStyleWeight weight = Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        _typeface = SKTypeface.FromFamilyName(FontFamily, weight, SKFontStyleWidth.Normal, slant);
        _font = new SKFont(_typeface, FontSize * DpiScale);
    }
}
