// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Events;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;

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
    private KxColor _foregroundColor = new(255, 255, 255);
    private KxColor _backgroundColor = new(16, 16, 16);
    private KxColor _borderColor = new(255, 215, 0);
    private KxColor _scrollBarColor = new(124, 110, 75, 160);
    private KxColor _glowColor = new(255, 255, 255, 180);

    private string _text;
    private string _fontFamily = "Segoe UI";
    private float _fontSize = DefaultFontSize;
    private bool _bold;
    private bool _italic;
    private SKTypeface? _typeface;
    private SKTypeface? _customTypeface;
    private SKFont? _font;
    private int _scrollOffset;
    private bool _isDraggingScrollMarker;
    private int _dragStartY;
    private int _scrollOffsetAtDragStart;
    private SKRect _scrollMarkerRect;
    private int _caretIndex;
    private System.Threading.Timer? _caretTimer;
    private bool _showCaret;
    private int _caretBlinkIntervalMs = 500;
    private bool _invertCaret = true;
    private float _caretWidth = 1f;
    private int _selectionStart = 0;
    private int _selectionEnd = 0;
    private bool _isSelecting = false;
    private readonly SKPaint _selectionPaint = new() { IsAntialias = false, Color = new SKColor(255, 255, 255, 60), Style = SKPaintStyle.Fill };
    private record TextSnapshot(string Text, int CaretIndex, int SelectionStart, int SelectionEnd);
    private readonly List<TextSnapshot> _undoStack = [];
    private readonly List<TextSnapshot> _redoStack = [];
    private const int UndoLimit = 200;

    private int GetCaretIndexFromPoint(Point point, Rectangle contentRect, List<string> wrappedLines, int lineHeight) {
        var y = point.Y - contentRect.Top + _scrollOffset;
        int line = (int)Math.Floor(y / (float)lineHeight);
        line = Math.Clamp(line, 0, Math.Max(0, wrappedLines.Count - 1));

        float x = point.X - contentRect.Left;
        if (x <= 0)
            return GetGlobalIndexForLineStart(wrappedLines, line);

        string textLine = wrappedLines[line];
        for (int i = 0; i <= textLine.Length; i++) {
            var left = _font?.MeasureText(textLine.AsSpan(0, i));
            if (left >= x)
                return GetGlobalIndexForLineStart(wrappedLines, line) + i;
        }
        return GetGlobalIndexForLineStart(wrappedLines, line) + textLine.Length;
    }

    private int GetGlobalIndexForLineStart(List<string> wrappedLines, int targetLine) {
        int idx = 0;
        for (int i = 0; i < targetLine && i < wrappedLines.Count; i++)
            idx += wrappedLines[i].Length + 1;
        return idx;
    }

    public int CaretBlinkInterval {
        get => _caretBlinkIntervalMs;
        set {
            int v = Math.Max(50, value);
            if (v == _caretBlinkIntervalMs)
                return;
            _caretBlinkIntervalMs = v;
            _caretTimer?.Change(_caretBlinkIntervalMs, _caretBlinkIntervalMs);
        }
    }

    public bool CaretInvert {
        get => _invertCaret;
        set {
            if (value == _invertCaret)
                return;
            _invertCaret = value;
            Invalidate();
        }
    }

    public float CaretWidth {
        get => _caretWidth;
        set {
            var v = Math.Max(0.5f, value);
            if (Math.Abs(v - _caretWidth) < 0.001f)
                return;
            _caretWidth = v;
            Invalidate();
        }
    }

    public TextBox(IVisualContext context, string id, string text) : base(context, id) {
        _text = text;
        Padding = new Kx.Sdk.UI.Layout.Thickness(6);
        UpdateFont();
    }

    public string Text {
        get => _text;
        set {
            _text = value ?? string.Empty;
            _caretIndex = Math.Clamp(_caretIndex, 0, _text.Length);
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

    /// <summary>
    /// Sets an explicit typeface that overrides family-name lookup for this text box.
    /// </summary>
    public void SetFontTypeface(SKTypeface? typeface) {
        _customTypeface?.Dispose();
        _customTypeface = typeface;
        UpdateFont();
        Invalidate();
    }

    public KxColor ForegroundColor {
        get => _foregroundColor;
        set {
            _foregroundColor = value;
            _textPaint.Color = value.ToSKColor();
            Invalidate();
        }
    }

    public KxColor BackgroundColor {
        get => _backgroundColor;
        set {
            _backgroundColor = value;
            _backgroundPaint.Color = value.ToSKColor();
            Invalidate();
        }
    }

    public KxColor BorderColor {
        get => _borderColor;
        set {
            _borderColor = value;
            _borderPaint.Color = value.ToSKColor();
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

    public KxColor ScrollBarColor {
        get => _scrollBarColor;
        set {
            _scrollBarColor = value;
            _scrollBarPaint.Color = value.ToSKColor();
            Invalidate();
        }
    }

    public bool Multiline { get; set; } = true;
    public bool ReadOnly { get; set; } = true;
    public override bool CanFocus => true;
    public bool GlowEnabled { get; set; }
    public float GlowRadius { get; set; } = 6f;
    public KxColor GlowColor {
        get => _glowColor;
        set {
            _glowColor = value;
            _glowPaint.Color = value.ToSKColor();
            Invalidate();
        }
    }

    /// <summary>
    /// Assigns the foreground color and returns the same text box for fluent configuration.
    /// </summary>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same text box instance.</returns>
    public TextBox WithForeground(KxColor color) {
        ForegroundColor = color;
        return this;
    }

    /// <summary>
    /// Assigns the background color and returns the same text box for fluent configuration.
    /// </summary>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same text box instance.</returns>
    public TextBox WithBackground(KxColor color) {
        BackgroundColor = color;
        return this;
    }

    /// <summary>
    /// Assigns border color and thickness and returns the same text box for fluent configuration.
    /// </summary>
    /// <param name="color">The border color to apply.</param>
    /// <param name="thickness">The border thickness to apply.</param>
    /// <returns>The same text box instance.</returns>
    public TextBox WithBorder(KxColor color, float thickness) {
        BorderColor = color;
        BorderThickness = thickness;
        return this;
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

        DesiredSize = new Size((int)(320 * dpi), (int)(180 * dpi));
    }

    protected override void OnDraw(IKxCanvas canvas) {
        var skCanvas = canvas.As<SKCanvas>();
        if (skCanvas is null)
            return;

        if (_font is null || !Visible)
            return;

        Rectangle rect = LayoutRect;
        Rectangle contentRect = ContentRect;

        skCanvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _backgroundPaint);

        if (GlowEnabled && BorderThickness > 0f) {
            using var glowImageFilter = SKImageFilter.CreateBlur(GlowRadius, GlowRadius);
            _glowPaint.ImageFilter = glowImageFilter;
            skCanvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _glowPaint);
            _glowPaint.ImageFilter = null;
        }

        if (BorderThickness > 0f)
            skCanvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _borderPaint);

        float availableTextWidth = Math.Max(8f, contentRect.Width - ScrollBarWidth - 4f);
        var wrappedWithStarts = GetWrappedLinesWithStartIndices(availableTextWidth);
        List<string> wrappedLines = [..wrappedWithStarts.Select(x => x.line)];
        int lineHeight = GetLineHeight();
        int totalTextHeight = wrappedLines.Count * lineHeight;
        int maxScroll = Math.Max(0, totalTextHeight - contentRect.Height);
        ClampScrollOffset(maxScroll);

        skCanvas.Save();
        skCanvas.ClipRect(new SKRect(contentRect.Left, contentRect.Top, contentRect.Right - ScrollBarWidth, contentRect.Bottom));

        float baseline = contentRect.Top - _font.Metrics.Ascent - _scrollOffset;
        foreach (var (line, start) in wrappedWithStarts) {
            skCanvas.DrawText(line, contentRect.Left, baseline, _font, _textPaint);
            baseline += lineHeight;
        }


        if (IsFocused && _showCaret) {
            float caretX = contentRect.Left;
            float caretY = contentRect.Top - _font.Metrics.Ascent - _scrollOffset;
            for (int i = 0; i < wrappedWithStarts.Count; i++) {
                var (line, start) = wrappedWithStarts[i];
                if (_caretIndex <= start + line.Length) {
                    var relative = _caretIndex - start;
                    string left = line[..Math.Max(0, relative)];
                    var xOffset = _font.MeasureText(left);
                    caretX = contentRect.Left + xOffset;
                    caretY = contentRect.Top + i * lineHeight - _font.Metrics.Ascent - _scrollOffset;
                    break;
                }
            }

            SKFontMetrics metrics = _font.Metrics;

            float caretTop = caretY + metrics.Ascent;
            float caretBottom = caretY + metrics.Descent;

            if (caretBottom >= contentRect.Top && caretTop <= contentRect.Bottom) {
                if (_selectionStart != _selectionEnd) {
                    int selStart = Math.Min(_selectionStart, _selectionEnd);
                    int selEnd = Math.Max(_selectionStart, _selectionEnd);
                    for (int li = 0; li < wrappedWithStarts.Count; li++) {
                        var (line, lineStart) = wrappedWithStarts[li];
                        int lineEnd = lineStart + line.Length;
                        int overlapStart = Math.Max(lineStart, selStart);
                        int overlapEnd = Math.Min(lineEnd, selEnd);
                        if (overlapStart < overlapEnd) {
                            string left = line[..Math.Max(0, overlapStart - lineStart)];
                            string mid = line.Substring(Math.Max(0, overlapStart - lineStart), overlapEnd - overlapStart);
                            float leftX = contentRect.Left + _font.MeasureText(left);
                            float midW = _font.MeasureText(mid);
                            float baselineForLine = contentRect.Top - metrics.Ascent - _scrollOffset + li * lineHeight;
                            float selTop = baselineForLine + metrics.Ascent;
                            float selHeight = (metrics.Descent - metrics.Ascent);
                            var selRect = new SKRect(leftX, selTop, leftX + midW, selTop + selHeight);
                            skCanvas.DrawRect(selRect, _selectionPaint);
                        }
                    }
                }

                if (_invertCaret) {
                    var caretRect = new SKRect(caretX - _caretWidth, caretTop, caretX + _caretWidth, caretBottom);
                    using var invertPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = false };
                    invertPaint.BlendMode = SKBlendMode.Difference;
                    invertPaint.Color = new SKColor(255, 255, 255);
                    skCanvas.DrawRect(caretRect, invertPaint);
                } else {
                    using var caretPaint = new SKPaint { Color = _textPaint.Color, StrokeWidth = Math.Max(_caretWidth, 1f * DpiScale), IsAntialias = true };
                    skCanvas.DrawLine(caretX, caretTop, caretX, caretBottom, caretPaint);
                }
            }
        }

        skCanvas.Restore();

        DrawScrollBar(skCanvas, contentRect, totalTextHeight, maxScroll);
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


        try {
            Context.UIElementManager.SetFocus(this);
        }
        catch { /* best-effort, don't throw during input handling */ }

        try {
            Rectangle content = ContentRect;
            float avail = Math.Max(8f, content.Width - ScrollBarWidth - 4f);
            var wrapped = GetWrappedLines(avail);
            int lineHeight = GetLineHeight();
            int idx = GetCaretIndexFromPoint(point, content, wrapped, lineHeight);
            _caretIndex = Math.Clamp(idx, 0, _text.Length);
            _selectionStart = _caretIndex;
            _selectionEnd = _caretIndex;
            _isSelecting = true;
            ResetCaretBlink();
            Invalidate();
        }
        catch { }

        return true;
    }

    public override bool DeleteWordLeft() {
        if (ReadOnly)
            return false;

        if (DeleteSelectionIfAny()) {
            ResetCaretBlink();
            Invalidate();
            return true;
        }

        if (_caretIndex <= 0)
            return false;

        PushUndoSnapshot();

        int j = _caretIndex;

        while (j > 0 && char.IsWhiteSpace(_text[j - 1]))
            j--;

        while (j > 0 && !char.IsWhiteSpace(_text[j - 1]))
            j--;

        int len = _caretIndex - j;
        if (len <= 0)
            return false;

        _text = _text.Remove(j, len);
        _caretIndex = j;
        _selectionStart = _selectionEnd = _caretIndex;
        _redoStack.Clear();
        ResetCaretBlink();
        Invalidate();
        return true;
    }

    public override bool DeleteWordRight() {
        if (ReadOnly)
            return false;

        if (DeleteSelectionIfAny()) {
            ResetCaretBlink();
            Invalidate();
            return true;
        }

        if (_caretIndex >= _text.Length)
            return false;

        PushUndoSnapshot();

        int j = _caretIndex;
        int n = _text.Length;
        while (j < n && char.IsWhiteSpace(_text[j]))
            j++;

        while (j < n && !char.IsWhiteSpace(_text[j]))
            j++;

        int len = j - _caretIndex;
        if (len <= 0)
            return false;

        _text = _text.Remove(_caretIndex, len);
        _selectionStart = _selectionEnd = _caretIndex;
        _redoStack.Clear();
        ResetCaretBlink();
        Invalidate();
        return true;
    }

    public override bool OnCopy() {
        if (_selectionStart == _selectionEnd)
            return false;
        int s = Math.Min(_selectionStart, _selectionEnd);
        int e = Math.Max(_selectionStart, _selectionEnd);
        var selected = _text[s..e ];
        try {
            System.Windows.Forms.Clipboard.SetText(selected);
            return true;
        }
        catch {
            return false;
        }
    }

    public override bool OnCut() {
        if (_selectionStart == _selectionEnd || ReadOnly)
            return false;
        int s = Math.Min(_selectionStart, _selectionEnd);
        int e = Math.Max(_selectionStart, _selectionEnd);
        var selected = _text[s..e ];
        try {
            System.Windows.Forms.Clipboard.SetText(selected);
            PushUndoSnapshot();
            _text = _text.Remove(s, e - s);
            _caretIndex = s;
            _selectionStart = _selectionEnd = _caretIndex;
            _redoStack.Clear();
            ResetCaretBlink();
            Invalidate();
            return true;
        }
        catch {
            return false;
        }
    }

    public override bool OnSelectAll() {
        _selectionStart = 0;
        _selectionEnd = _text.Length;
        _caretIndex = _selectionEnd;
        Invalidate();
        return true;
    }

    public override bool OnPaste(string text) {
        if (string.IsNullOrEmpty(text) || ReadOnly)
            return false;
        PushUndoSnapshot();
        DeleteSelectionIfAny();
        _text = _text.Insert(_caretIndex, text);
        _caretIndex = Math.Min(_text.Length, _caretIndex + text.Length);
        _redoStack.Clear();
        ResetCaretBlink();
        Invalidate();
        return true;
    }

    public override bool OnKeyDown(KeyCode key) {
        if (!IsFocused || ReadOnly)
            return false;

        try {
            switch (key) {
                case KeyCode.Backspace:
                if (DeleteSelectionIfAny()) {
                    Invalidate();
                    return true;
                }
                if (_caretIndex > 0) {
                    PushUndoSnapshot();
                    _text = _text.Remove(_caretIndex - 1, 1);
                    _caretIndex = Math.Max(0, _caretIndex - 1);
                    _redoStack.Clear();
                    ResetCaretBlink();
                    Invalidate();
                    return true;
                }

                return false;
                case KeyCode.Left:
                if (_caretIndex > 0) {
                    _caretIndex--;
                    Invalidate();
                    return true;
                }
                return false;
                case KeyCode.Right:
                if (_caretIndex < _text.Length) {
                    _caretIndex++;
                    ResetCaretBlink();
                    Invalidate();
                    return true;
                }
                return false;
                case KeyCode.Enter:
                if (Multiline) {
                    if (!DeleteSelectionIfAny()) {
                        PushUndoSnapshot();
                        _text = _text.Insert(_caretIndex, "\n");
                        _caretIndex++;
                        _redoStack.Clear();
                    }
                    ResetCaretBlink();
                    Invalidate();
                    return true;
                }
                return false;
                case KeyCode.Space: // TODO: one button press inserts 2x space in some cases, investigate
                if (!DeleteSelectionIfAny()) {
                    PushUndoSnapshot();
                    _text = _text.Insert(_caretIndex, " ");
                    _caretIndex++;
                    _redoStack.Clear();
                }
                ResetCaretBlink();
                Invalidate();
                return true;
                case KeyCode.Tab:
                if (!DeleteSelectionIfAny()) {
                    PushUndoSnapshot();
                    _text = _text.Insert(_caretIndex, "\t");
                    _caretIndex++;
                    _redoStack.Clear();
                }
                ResetCaretBlink();
                Invalidate();
                return true;
                case KeyCode.Delete:
                if (DeleteSelectionIfAny()) {
                    Invalidate();
                    return true;
                }

                if (_caretIndex < _text.Length) {
                    PushUndoSnapshot();
                    _text = _text.Remove(_caretIndex, 1);
                    _redoStack.Clear();
                    ResetCaretBlink();
                    Invalidate();
                    return true;
                }
                return false;
                default:
                break;
            }
        }
        catch { /* ignore errors in best-effort input handling */ }

        return false;
    }


    public override void OnFocusGained() {
        base.OnFocusGained();
        _showCaret = true;
        _caretTimer?.Dispose();
        _caretTimer = new System.Threading.Timer(_ => {
            _showCaret = !_showCaret;
            try {
                Context.UiThread.BeginInvoke((Action)(() => Invalidate()));
            }
            catch {
            }
        }, null, _caretBlinkIntervalMs, _caretBlinkIntervalMs);
    }

    public override void OnFocusLost() {
        base.OnFocusLost();
        _caretTimer?.Dispose();
        _caretTimer = null;
        _showCaret = false;
        Invalidate();
    }

    public override bool OnTextInput(string text) {
        if (!IsFocused || ReadOnly || string.IsNullOrEmpty(text))
            return false;

        var cleaned = new string([.. text.Where(c => !char.IsControl(c))]);
        if (string.IsNullOrEmpty(cleaned))
            return false;

        PushUndoSnapshot();
        if (!DeleteSelectionIfAny()) {
            _text = _text.Insert(_caretIndex, cleaned);
            _caretIndex = Math.Min(_text.Length, _caretIndex + cleaned.Length);
        }
        _redoStack.Clear();
        ResetCaretBlink();
        Invalidate();
        return true;
    }

    private bool DeleteSelectionIfAny() {
        if (_selectionStart == _selectionEnd)
            return false;

        PushUndoSnapshot();

        int s = Math.Min(_selectionStart, _selectionEnd);
        int e = Math.Max(_selectionStart, _selectionEnd);
        _text = _text.Remove(s, e - s);
        _caretIndex = s;
        _selectionStart = _selectionEnd = _caretIndex;

        _redoStack.Clear();
        return true;
    }

    private void PushUndoSnapshot() {
        try {
            var snap = new TextSnapshot(_text ?? string.Empty, _caretIndex, _selectionStart, _selectionEnd);
            if (_undoStack.Count > 0) {
                var last = _undoStack[^1];
                if (last.Text == snap.Text && last.CaretIndex == snap.CaretIndex && last.SelectionStart == snap.SelectionStart && last.SelectionEnd == snap.SelectionEnd)
                    return;
            }

            _undoStack.Add(snap);
            if (_undoStack.Count > UndoLimit)
                _undoStack.RemoveAt(0);
        }
        catch { }
    }

    public override bool OnUndo() {
        if (_undoStack.Count == 0)
            return false;

        _redoStack.Add(new TextSnapshot(_text ?? string.Empty, _caretIndex, _selectionStart, _selectionEnd));
        var snap = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        ApplySnapshot(snap);
        return true;
    }

    public override bool OnRedo() {
        if (_redoStack.Count == 0)
            return false;

        _undoStack.Add(new TextSnapshot(_text ?? string.Empty, _caretIndex, _selectionStart, _selectionEnd));
        var snap = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        ApplySnapshot(snap);
        return true;
    }

    private void ApplySnapshot(TextSnapshot snap) {
        _text = snap.Text ?? string.Empty;
        _caretIndex = Math.Clamp(snap.CaretIndex, 0, _text.Length);
        _selectionStart = Math.Clamp(snap.SelectionStart, 0, _text.Length);
        _selectionEnd = Math.Clamp(snap.SelectionEnd, 0, _text.Length);
        ResetCaretBlink();
        Invalidate();
    }

    private void ResetCaretBlink() {
        _showCaret = true;
        try {
            Context.UiThread.BeginInvoke((Action)(() => Invalidate()));
        }
        catch { }

        _caretTimer?.Change(_caretBlinkIntervalMs, _caretBlinkIntervalMs);
    }

    public override bool OnMouseMove(Point point) {
        if (_isSelecting) {
            try {
                Rectangle selContentRect = ContentRect;
                float selAvailableTextWidth = Math.Max(8f, selContentRect.Width - ScrollBarWidth - 4f);
                var selWrapped = GetWrappedLines(selAvailableTextWidth);
                int selLineHeight = GetLineHeight();
                int idx = GetCaretIndexFromPoint(point, selContentRect, selWrapped, selLineHeight);
                _selectionEnd = Math.Clamp(idx, 0, _text.Length);
                _caretIndex = _selectionEnd;
                Invalidate();
                return true;
            }
            catch { }
        }

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
        if (_isSelecting) {
            Rectangle contentRect = ContentRect;
            float availableTextWidth = Math.Max(8f, contentRect.Width - ScrollBarWidth - 4f);
            var wrapped = GetWrappedLines(availableTextWidth);
            int lineHeight = GetLineHeight();
            int idx = GetCaretIndexFromPoint(point, contentRect, wrapped, lineHeight);
            _selectionEnd = Math.Clamp(idx, 0, _text.Length);
            _caretIndex = _selectionEnd;
            _isSelecting = false;
            Invalidate();
            return true;
        }

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
            _customTypeface?.Dispose();
            _typeface?.Dispose();
            _textPaint.Dispose();
            _backgroundPaint.Dispose();
            _borderPaint.Dispose();
            _scrollBarPaint.Dispose();
            _glowPaint.Dispose();
            _caretTimer?.Dispose();
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

    private List<(string line, int start)> GetWrappedLinesWithStartIndices(float availableWidth) {
        List<(string, int)> lines = [];
        if (_font is null)
            return lines;

        string[] paragraphs = Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int globalIndex = 0;
        foreach (string paragraph in paragraphs) {
            if (!Multiline) {
                lines.Add((paragraph, globalIndex));
                globalIndex += paragraph.Length + 1;
                continue;
            }

            if (string.IsNullOrEmpty(paragraph)) {
                lines.Add((string.Empty, globalIndex));
                globalIndex += 1;
                continue;
            }

            string currentLine = string.Empty;
            int lineStart = globalIndex;
            foreach (string word in paragraph.Split(' ', StringSplitOptions.None)) {
                string candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                if (string.IsNullOrEmpty(currentLine) || _font.MeasureText(candidate) <= availableWidth) {
                    currentLine = candidate;
                    continue;
                }

                lines.Add((currentLine, lineStart));
                lineStart += currentLine.Length + 1;
                currentLine = word;
            }

            lines.Add((currentLine, lineStart));
            globalIndex = lineStart + currentLine.Length + 1;
        }

        if (lines.Count == 0)
            lines.Add((string.Empty, 0));

        return lines;
    }

    private void UpdateFont() {
        _font?.Dispose();

        if (_customTypeface is null) {
            _typeface?.Dispose();

            SKFontStyleWeight weight = Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            SKFontStyleSlant slant = Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

            _typeface = SKTypeface.FromFamilyName(FontFamily, weight, SKFontStyleWidth.Normal, slant);
        } else {
            _typeface?.Dispose();
            _typeface = null;
        }

        _font = new SKFont(_customTypeface ?? _typeface ?? SKTypeface.Default, FontSize * DpiScale);
    }
}
