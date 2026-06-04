// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Core.Extensions;
using Kx.Sdk.Rendering;
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

    private KxColor _textColor = SKColors.White.ToKxColor();
    private KxColor _backgroundColor = new(16, 16, 16);
    private KxColor _borderColor = new(124, 110, 75);
    private KxColor _selectedItemColor = new(124, 110, 75, 180);
    private KxColor _hoveredItemColor = new(70, 70, 70, 180);
    private KxColor _selectedItemBorderColor = new(232, 217, 180, 220);
    private KxColor _separatorColor = new(92, 82, 56, 120);
    private KxColor _scrollBarColor = new(124, 110, 75, 180);
    private KxColor _glowColor = new(255, 255, 255, 180);
    private float _borderThickness = 2f;
    private float _selectedItemBorderThickness = 1f;
    private float _separatorThickness = 1f;

    private readonly List<string> _items = [];
    private string _fontFamily = "Segoe UI";
    private float _fontSize = DefaultFontSize;
    private bool _bold;
    private bool _italic;
    private SKTypeface? _typeface;
    private SKTypeface? _customTypeface;
    private SKFont? _font;
    private int _selectedIndex = -1;
    private int _hoveredIndex = -1;
    private int _firstVisibleIndex;
    private KxRect _scrollMarkerRect;
    private bool _isDraggingScrollMarker;
    private bool _isHoveringScrollMarker;
    private int _dragStartY;
    private int _firstVisibleIndexAtDragStart;

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

    /// <summary>
    /// Sets an explicit typeface that overrides family-name lookup for this list box.
    /// </summary>
    public void SetFontTypeface(SKTypeface? typeface) {
        _customTypeface?.Dispose();
        _customTypeface = typeface;
        UpdateFont();
        Invalidate();
    }

    public KxColor ForegroundColor {
        get => _textColor;
        set {
            _textColor = value;
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

    public KxColor SelectedItemColor {
        get => _selectedItemColor;
        set {
            _selectedItemColor = value;
            Invalidate();
        }
    }

    public KxColor HoveredItemColor {
        get => _hoveredItemColor;
        set {
            _hoveredItemColor = value;
            Invalidate();
        }
    }

    public KxColor SelectedItemBorderColor {
        get => _selectedItemBorderColor;
        set {
            _selectedItemBorderColor = value;
            Invalidate();
        }
    }

    public KxColor SeparatorColor {
        get => _separatorColor;
        set {
            _separatorColor = value;
            Invalidate();
        }
    }

    public KxColor ScrollBarColor {
        get => _scrollBarColor;
        set {
            _scrollBarColor = value;
            Invalidate();
        }
    }

    public bool GlowEnabled { get; set; }

    public KxColor GlowColor {
        get => _glowColor;
        set {
            _glowColor = value;
            Invalidate();
        }
    }

    public float GlowRadius { get; set; } = 6f;

    /// <summary>
    /// Assigns the foreground color and returns the same list box for fluent configuration.
    /// </summary>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same list box instance.</returns>
    public ListBox WithForeground(KxColor color) {
        ForegroundColor = color;
        return this;
    }

    /// <summary>
    /// Assigns the background color and returns the same list box for fluent configuration.
    /// </summary>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same list box instance.</returns>
    public ListBox WithBackground(KxColor color) {
        BackgroundColor = color;
        return this;
    }

    /// <summary>
    /// Assigns border color and thickness and returns the same list box for fluent configuration.
    /// </summary>
    /// <param name="color">The border color to apply.</param>
    /// <param name="thickness">The border thickness to apply.</param>
    /// <returns>The same list box instance.</returns>
    public ListBox WithBorder(KxColor color, float thickness) {
        BorderColor = color;
        BorderThickness = thickness;
        return this;
    }

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

    protected override void OnDraw(IKxCanvas canvas) {
        var skCanvas = canvas.As<SKCanvas>();
        if (skCanvas is null)
            return;

        if (_font is null || !Visible)
            return;

        Rectangle rect = LayoutRect;
        Rectangle contentRect = ContentRect;

        canvas.DrawRect(rect.Left, rect.Top, rect.Right, rect.Bottom, _backgroundColor);

        if (GlowEnabled && BorderThickness > 0f) {
            using var glowImageFilter = SKImageFilter.CreateBlur(GlowRadius, GlowRadius);
            using var glowPaint = new SKPaint {
                IsAntialias = true,
                Color = _glowColor.ToSKColor(),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = _borderThickness,
                ImageFilter = glowImageFilter
            };
            skCanvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, glowPaint);
        }

        if (BorderThickness > 0f)
            canvas.DrawRectStroke(rect.Left, rect.Top, rect.Right, rect.Bottom, _borderColor, _borderThickness);

        int itemHeight = GetItemHeight();
        int availableListWidth = Math.Max(16, contentRect.Width - ScrollBarWidth - 4);
        int visibleItemCount = Math.Max(1, contentRect.Height / Math.Max(1, itemHeight));
        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex, 0, Math.Max(0, _items.Count - visibleItemCount));

        skCanvas.Save();
        skCanvas.ClipRect(new SKRect(contentRect.Left, contentRect.Top, contentRect.Right - ScrollBarWidth, contentRect.Bottom));

        for (int slot = 0; slot < visibleItemCount; slot++) {
            int itemIndex = _firstVisibleIndex + slot;
            if (itemIndex >= _items.Count)
                break;

            var itemRect = new Rectangle(contentRect.Left, contentRect.Top + slot * itemHeight, availableListWidth, itemHeight - ItemGap);
            if (itemIndex == _selectedIndex) {
                canvas.DrawRect(itemRect.Left, itemRect.Top, itemRect.Right, itemRect.Bottom, _selectedItemColor);
                canvas.DrawRectStroke(itemRect.Left + 0.5f, itemRect.Top + 0.5f, itemRect.Right - 0.5f, itemRect.Bottom - 0.5f, _selectedItemBorderColor, _selectedItemBorderThickness);
            }
            else if (itemIndex == _hoveredIndex)
                canvas.DrawRect(itemRect.Left, itemRect.Top, itemRect.Right, itemRect.Bottom, _hoveredItemColor);

            string itemText = TruncateText(_items[itemIndex], Math.Max(20f, itemRect.Width - ItemPadding * 2f));
            float baseline = itemRect.Top + ItemPadding - _font.Metrics.Ascent;
            canvas.DrawText(itemText, itemRect.Left + ItemPadding, baseline, _font.Size, _textColor, _fontFamily, _bold, _italic);

            if (slot < visibleItemCount - 1 && itemIndex < _items.Count - 1) {
                float separatorY = itemRect.Bottom + (ItemGap / 2f);
                canvas.DrawLine(itemRect.Left + ItemPadding, separatorY, itemRect.Right - ItemPadding, separatorY, _separatorColor, _separatorThickness);
            }
        }

        skCanvas.Restore();

        DrawScrollBar(canvas, contentRect, itemHeight, visibleItemCount);
    }

    public override bool OnMouseDown(Point point) {
        if (!Bounds.Contains(point))
            return false;

        if (IsPointInScrollBar(point)) {
            if (IsPointInScrollMarker(point)) {
                _isDraggingScrollMarker = true;
                _isHoveringScrollMarker = true;
                _dragStartY = point.Y;
                _firstVisibleIndexAtDragStart = _firstVisibleIndex;
                return true;
            }

            if (point.Y < _scrollMarkerRect.Top)
                ScrollByViewport(-1);
            else if (point.Y > _scrollMarkerRect.Bottom)
                ScrollByViewport(1);

            return true;
        }

        int itemIndex = GetItemIndexAt(point);
        if (itemIndex < 0)
            return false;

        SetSelectedIndex(itemIndex);
        return true;
    }

    public override bool OnMouseMove(Point point) {
        if (_isDraggingScrollMarker) {
            if (!TryGetScrollMetrics(out int visibleItemCount, out int maxFirstVisibleIndex, out _))
                return false;

            float markerHeight = Math.Max(MinimumScrollMarkerHeight, ContentRect.Height * (visibleItemCount / (float)_items.Count));
            float markerTravel = Math.Max(1f, ContentRect.Height - markerHeight);
            float markerDelta = point.Y - _dragStartY;
            int indexDelta = (int)Math.Round((markerDelta / markerTravel) * maxFirstVisibleIndex);
            int nextFirstVisibleIndex = Math.Clamp(_firstVisibleIndexAtDragStart + indexDelta, 0, maxFirstVisibleIndex);

            if (nextFirstVisibleIndex == _firstVisibleIndex)
                return true;

            _firstVisibleIndex = nextFirstVisibleIndex;
            Invalidate();
            return true;
        }

        bool isHoveringScrollMarker = IsPointInScrollMarker(point);
        if (_isHoveringScrollMarker != isHoveringScrollMarker) {
            _isHoveringScrollMarker = isHoveringScrollMarker;
            Invalidate();
        }

        if (IsPointInScrollBar(point)) {
            if (_hoveredIndex >= 0) {
                _hoveredIndex = -1;
                Invalidate();
            }

            return true;
        }

        int hoveredIndex = Bounds.Contains(point)
            ? GetItemIndexAt(point)
            : -1;

        if (_hoveredIndex == hoveredIndex)
            return hoveredIndex >= 0;

        _hoveredIndex = hoveredIndex;
        Invalidate();
        return true;
    }

    public override bool OnMouseUp(Point point) {
        if (!_isDraggingScrollMarker)
            return false;

        _isDraggingScrollMarker = false;
        _isHoveringScrollMarker = IsPointInScrollMarker(point);
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
        _isDraggingScrollMarker = false;
        _isHoveringScrollMarker = false;
        Invalidate();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _font?.Dispose();
            _customTypeface?.Dispose();
            _typeface?.Dispose();
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

        if (point.X >= contentRect.Right - ScrollBarWidth)
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

    private void DrawScrollBar(IKxCanvas canvas, Rectangle contentRect, int itemHeight, int visibleItemCount) {
        _scrollMarkerRect = new KxRect(0f, 0f, 0f, 0f);

        int maxFirstVisibleIndex = Math.Max(0, _items.Count - visibleItemCount);
        if (maxFirstVisibleIndex <= 0)
            return;

        float visibleHeight = contentRect.Height;
        float markerHeight = Math.Max(MinimumScrollMarkerHeight, visibleHeight * (visibleItemCount / (float)_items.Count));
        float markerTravel = Math.Max(1f, visibleHeight - markerHeight);
        float markerY = contentRect.Top + (_firstVisibleIndex / (float)maxFirstVisibleIndex) * markerTravel;

        _scrollMarkerRect = new KxRect(contentRect.Right - ScrollBarWidth + 1, markerY, contentRect.Right - 1, markerY + markerHeight);
        canvas.DrawRect(_scrollMarkerRect.Left, _scrollMarkerRect.Top, _scrollMarkerRect.Right, _scrollMarkerRect.Bottom, ResolveScrollMarkerColor());
    }

    private KxColor ResolveScrollMarkerColor() {
        if (_isDraggingScrollMarker)
            return new KxColor(_scrollBarColor.R, _scrollBarColor.G, _scrollBarColor.B, 255);

        if (_isHoveringScrollMarker)
            return new KxColor(_scrollBarColor.R, _scrollBarColor.G, _scrollBarColor.B, (byte)Math.Min(255, _scrollBarColor.A + 40));

        return _scrollBarColor;
    }

    private bool IsPointInScrollBar(Point point) {
        Rectangle contentRect = ContentRect;
        if (!contentRect.Contains(point))
            return false;

        return point.X >= contentRect.Right - ScrollBarWidth;
    }

    private bool IsPointInScrollMarker(Point point) {
        return point.X >= _scrollMarkerRect.Left
            && point.X <= _scrollMarkerRect.Right
            && point.Y >= _scrollMarkerRect.Top
            && point.Y <= _scrollMarkerRect.Bottom;
    }

    private void ScrollByViewport(int direction) {
        if (!TryGetScrollMetrics(out int visibleItemCount, out int maxFirstVisibleIndex, out _))
            return;

        int nextFirstVisibleIndex = Math.Clamp(_firstVisibleIndex + (visibleItemCount * direction), 0, maxFirstVisibleIndex);
        if (nextFirstVisibleIndex == _firstVisibleIndex)
            return;

        _firstVisibleIndex = nextFirstVisibleIndex;
        Invalidate();
    }

    private bool TryGetScrollMetrics(out int visibleItemCount, out int maxFirstVisibleIndex, out int itemHeight) {
        itemHeight = GetItemHeight();
        visibleItemCount = Math.Max(1, ContentRect.Height / Math.Max(1, itemHeight));
        maxFirstVisibleIndex = Math.Max(0, _items.Count - visibleItemCount);
        return maxFirstVisibleIndex > 0;
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
        _font?.Dispose();

        if (_customTypeface is null) {
            var weight = _bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            var slant = _italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

            _typeface?.Dispose();
            _typeface = SKTypeface.FromFamilyName(_fontFamily, weight, SKFontStyleWidth.Normal, slant);
        }
        else {
            _typeface?.Dispose();
            _typeface = null;
        }

        _font = new SKFont(_customTypeface ?? _typeface ?? SKTypeface.Default, _fontSize * DpiScale);
        _selectedItemBorderThickness = Math.Max(1f, DpiScale);
        _separatorThickness = Math.Max(1f, DpiScale * 0.75f);
        _borderThickness = Math.Max(1f, _borderThickness);
    }
}
