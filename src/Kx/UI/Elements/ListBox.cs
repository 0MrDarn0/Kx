// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;

using SkiaSharp;

namespace Kx.UI.Elements;

public sealed class ListBox : UIElement {
    private const float DefaultFontSize = 12f;
    private const int ItemPadding = 6;
    private const int ItemGap = 3;
    private const int ScrollBarWidth = 8;
    private const int MinimumScrollMarkerHeight = 20;

    private readonly SKPaint _textPaint = new() { IsAntialias = true, Color = SKColors.White };
    private readonly SKPaint _backgroundPaint = new() { IsAntialias = true, Color = new SKColor(16, 16, 16) };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Color = new SKColor(124, 110, 75), Style = SKPaintStyle.Stroke, StrokeWidth = 2f };
    private readonly SKPaint _selectedItemPaint = new() { IsAntialias = true, Color = new SKColor(124, 110, 75, 180), Style = SKPaintStyle.Fill };
    private readonly SKPaint _hoveredItemPaint = new() { IsAntialias = true, Color = new SKColor(70, 70, 70, 180), Style = SKPaintStyle.Fill };
    private readonly SKPaint _selectedItemBorderPaint = new() { IsAntialias = true, Color = new SKColor(232, 217, 180, 220), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
    private readonly SKPaint _separatorPaint = new() { IsAntialias = true, Color = new SKColor(92, 82, 56, 120), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
    private readonly SKPaint _scrollBarPaint = new() { IsAntialias = true, Color = new SKColor(124, 110, 75, 180), Style = SKPaintStyle.Fill };
    private readonly SKPaint _glowPaint = new() { IsAntialias = true, Color = new SKColor(255, 255, 255, 180), Style = SKPaintStyle.Stroke, StrokeWidth = 2f };

    private readonly List<string> _items = [];
    private string _fontFamily = "Segoe UI";
    private float _fontSize = DefaultFontSize;
    private bool _bold;
    private bool _italic;
    private SKTypeface? _typeface;
    private SKFont? _font;
    private int _selectedIndex = -1;
    private int _hoveredIndex = -1;
    private int _firstVisibleIndex;
    private SKRect _scrollMarkerRect;

    public ListBox(IVisualContext context, string id) : base(context, id) {
        Padding = new Kx.Sdk.UI.Layout.Thickness(4);
        UpdateFont();
    }

    public event Action<int, string?>? SelectedIndexChanged;

    public IReadOnlyList<string> Items => _items;

    public int SelectedIndex => _selectedIndex;

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
            _borderPaint.StrokeWidth = Math.Max(0f, value);
            Invalidate();
        }
    }

    public SKColor SelectedItemColor {
        get => _selectedItemPaint.Color;
        set {
            _selectedItemPaint.Color = value;
            Invalidate();
        }
    }

    public SKColor HoveredItemColor {
        get => _hoveredItemPaint.Color;
        set {
            _hoveredItemPaint.Color = value;
            Invalidate();
        }
    }

    public SKColor SelectedItemBorderColor {
        get => _selectedItemBorderPaint.Color;
        set {
            _selectedItemBorderPaint.Color = value;
            Invalidate();
        }
    }

    public SKColor SeparatorColor {
        get => _separatorPaint.Color;
        set {
            _separatorPaint.Color = value;
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

    public bool GlowEnabled { get; set; }

    public SKColor GlowColor {
        get => _glowPaint.Color;
        set {
            _glowPaint.Color = value;
            Invalidate();
        }
    }

    public float GlowRadius { get; set; } = 6f;

    /// <summary>
    /// Replaces the current list items.
    /// </summary>
    public void SetItems(IEnumerable<string>? items) {
        _items.Clear();

        if (items is not null)
            _items.AddRange(items.Where(item => item is not null));

        _hoveredIndex = -1;
        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex, 0, Math.Max(0, _items.Count - 1));

        if (_items.Count == 0) {
            SetSelectedIndex(-1);
            return;
        }

        if (_selectedIndex < 0 || _selectedIndex >= _items.Count) {
            SetSelectedIndex(0);
            return;
        }

        EnsureSelectedItemVisible();
        Invalidate();
    }

    /// <summary>
    /// Updates the selected item index.
    /// </summary>
    public void SetSelectedIndex(int index, bool notify = true) {
        int normalizedIndex = _items.Count == 0
            ? -1
            : Math.Clamp(index, 0, _items.Count - 1);

        if (_selectedIndex == normalizedIndex)
            return;

        _selectedIndex = normalizedIndex;
        EnsureSelectedItemVisible();
        Invalidate();

        if (notify)
            SelectedIndexChanged?.Invoke(_selectedIndex, _selectedIndex >= 0 ? _items[_selectedIndex] : null);
    }

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

        DesiredSize = new Size((int)(240 * dpi), (int)(260 * dpi));
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

        int itemHeight = GetItemHeight();
        int availableListWidth = Math.Max(16, contentRect.Width - ScrollBarWidth - 4);
        int visibleItemCount = Math.Max(1, contentRect.Height / Math.Max(1, itemHeight));
        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex, 0, Math.Max(0, _items.Count - visibleItemCount));

        canvas.Save();
        canvas.ClipRect(new SKRect(contentRect.Left, contentRect.Top, contentRect.Right - ScrollBarWidth, contentRect.Bottom));

        for (int slot = 0; slot < visibleItemCount; slot++) {
            int itemIndex = _firstVisibleIndex + slot;
            if (itemIndex >= _items.Count)
                break;

            var itemRect = new Rectangle(contentRect.Left, contentRect.Top + slot * itemHeight, availableListWidth, itemHeight - ItemGap);
            if (itemIndex == _selectedIndex) {
                canvas.DrawRect(itemRect.Left, itemRect.Top, itemRect.Width, itemRect.Height, _selectedItemPaint);
                canvas.DrawRect(itemRect.Left + 0.5f, itemRect.Top + 0.5f, itemRect.Width - 1f, itemRect.Height - 1f, _selectedItemBorderPaint);
            }
            else if (itemIndex == _hoveredIndex)
                canvas.DrawRect(itemRect.Left, itemRect.Top, itemRect.Width, itemRect.Height, _hoveredItemPaint);

            string itemText = TruncateText(_items[itemIndex], Math.Max(20f, itemRect.Width - ItemPadding * 2f));
            float baseline = itemRect.Top + ItemPadding - _font.Metrics.Ascent;
            canvas.DrawText(itemText, itemRect.Left + ItemPadding, baseline, _font, _textPaint);

            if (slot < visibleItemCount - 1 && itemIndex < _items.Count - 1) {
                float separatorY = itemRect.Bottom + (ItemGap / 2f);
                canvas.DrawLine(itemRect.Left + ItemPadding, separatorY, itemRect.Right - ItemPadding, separatorY, _separatorPaint);
            }
        }

        canvas.Restore();

        DrawScrollBar(canvas, contentRect, itemHeight, visibleItemCount);
    }

    public override bool OnMouseDown(Point point) {
        if (!Bounds.Contains(point))
            return false;

        int itemIndex = GetItemIndexAt(point);
        if (itemIndex < 0)
            return false;

        SetSelectedIndex(itemIndex);
        return true;
    }

    public override bool OnMouseMove(Point point) {
        int hoveredIndex = Bounds.Contains(point)
            ? GetItemIndexAt(point)
            : -1;

        if (_hoveredIndex == hoveredIndex)
            return hoveredIndex >= 0;

        _hoveredIndex = hoveredIndex;
        Invalidate();
        return true;
    }

    public override bool OnMouseWheel(int delta, Point point) {
        if (!Bounds.Contains(point))
            return false;

        int itemHeight = GetItemHeight();
        int visibleItemCount = Math.Max(1, ContentRect.Height / Math.Max(1, itemHeight));
        int maxFirstVisibleIndex = Math.Max(0, _items.Count - visibleItemCount);
        if (maxFirstVisibleIndex == 0)
            return false;

        int nextFirstVisibleIndex = Math.Clamp(_firstVisibleIndex - Math.Sign(delta), 0, maxFirstVisibleIndex);
        if (nextFirstVisibleIndex == _firstVisibleIndex)
            return false;

        _firstVisibleIndex = nextFirstVisibleIndex;
        Invalidate();
        return true;
    }

    public override void OnFocusLost() {
        _hoveredIndex = -1;
        Invalidate();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _font?.Dispose();
            _typeface?.Dispose();
            _textPaint.Dispose();
            _backgroundPaint.Dispose();
            _borderPaint.Dispose();
            _selectedItemPaint.Dispose();
            _hoveredItemPaint.Dispose();
            _selectedItemBorderPaint.Dispose();
            _separatorPaint.Dispose();
            _scrollBarPaint.Dispose();
            _glowPaint.Dispose();
        }

        base.Dispose(disposing);
    }

    private int GetItemHeight() {
        if (_font is null)
            return 20;

        SKFontMetrics metrics = _font.Metrics;
        return Math.Max(22, (int)Math.Ceiling((metrics.Descent - metrics.Ascent) + ItemPadding * 2f + ItemGap));
    }

    private int GetItemIndexAt(Point point) {
        Rectangle contentRect = ContentRect;
        if (!contentRect.Contains(point))
            return -1;

        int itemHeight = GetItemHeight();
        int slot = (point.Y - contentRect.Top) / Math.Max(1, itemHeight);
        int itemIndex = _firstVisibleIndex + slot;
        return itemIndex >= 0 && itemIndex < _items.Count
            ? itemIndex
            : -1;
    }

    private void EnsureSelectedItemVisible() {
        if (_selectedIndex < 0)
            return;

        int itemHeight = GetItemHeight();
        int visibleItemCount = Math.Max(1, ContentRect.Height / Math.Max(1, itemHeight));
        if (_selectedIndex < _firstVisibleIndex)
            _firstVisibleIndex = _selectedIndex;
        else if (_selectedIndex >= _firstVisibleIndex + visibleItemCount)
            _firstVisibleIndex = _selectedIndex - visibleItemCount + 1;
    }

    private void DrawScrollBar(SKCanvas canvas, Rectangle contentRect, int itemHeight, int visibleItemCount) {
        _scrollMarkerRect = SKRect.Empty;

        int maxFirstVisibleIndex = Math.Max(0, _items.Count - visibleItemCount);
        if (maxFirstVisibleIndex <= 0)
            return;

        float visibleHeight = contentRect.Height;
        float markerHeight = Math.Max(MinimumScrollMarkerHeight, visibleHeight * (visibleItemCount / (float)_items.Count));
        float markerTravel = Math.Max(1f, visibleHeight - markerHeight);
        float markerY = contentRect.Top + (_firstVisibleIndex / (float)maxFirstVisibleIndex) * markerTravel;

        _scrollMarkerRect = new SKRect(contentRect.Right - ScrollBarWidth + 1, markerY, contentRect.Right - 1, markerY + markerHeight);
        canvas.DrawRect(_scrollMarkerRect, _scrollBarPaint);
    }

    private string TruncateText(string text, float maxWidth) {
        if (_font is null || string.IsNullOrEmpty(text))
            return text;

        _font.MeasureText(text, out SKRect bounds);
        if (bounds.Width <= maxWidth)
            return text;

        const string ellipsis = "…";
        int length = text.Length;

        while (length > 1) {
            string candidate = string.Concat(text.AsSpan(0, length), ellipsis);
            _font.MeasureText(candidate, out bounds);
            if (bounds.Width <= maxWidth)
                return candidate;

            length--;
        }

        return ellipsis;
    }

    private void UpdateFont() {
        var weight = _bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var slant = _italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        _font?.Dispose();
        _typeface?.Dispose();

        _typeface = SKTypeface.FromFamilyName(_fontFamily, weight, SKFontStyleWidth.Normal, slant);
        _font = new SKFont(_typeface ?? SKTypeface.Default, _fontSize * DpiScale);
        _selectedItemBorderPaint.StrokeWidth = Math.Max(1f, DpiScale);
        _separatorPaint.StrokeWidth = Math.Max(1f, DpiScale * 0.75f);
        _borderPaint.StrokeWidth = Math.Max(1f, _borderPaint.StrokeWidth);
        _glowPaint.StrokeWidth = _borderPaint.StrokeWidth;
    }
}
