// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI.Themes;
using Kx.UI.Platform;

using SkiaSharp;

namespace KxEdit;

internal sealed class EditorPreviewCanvas : UIElement {
    private const int VirtualWidth = 1100;
    private const int VirtualHeight = 760;

    private enum DragMode {
        None,
        Move,
        Resize
    }

    private readonly SKPaint _canvasPaint = new() { IsAntialias = true, Color = new SKColor(18, 22, 28) };
    private readonly SKPaint _windowPaint = new() { IsAntialias = true };
    private readonly SKPaint _titleBarPaint = new() { IsAntialias = true };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f };
    private readonly SKPaint _controlPaint = new() { IsAntialias = true };
    private readonly SKPaint _selectionPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = new SKColor(255, 215, 96) };
    private readonly SKPaint _handlePaint = new() { IsAntialias = true, Color = new SKColor(255, 215, 96) };
    private readonly SKPaint _textPaint = new() { IsAntialias = true, Color = SKColors.White };
    private readonly SKFont _titleFont = new(SKTypeface.Default, 14);
    private readonly SKFont _bodyFont = new(SKTypeface.Default, 11);

    private readonly List<PreviewHit> _hits = [];

    private MarkupEditorDocument? _document;
    private MarkupEditorControl? _selectedControl;
    private bool _frameSelected = true;
    private DragMode _dragMode;
    private Point _dragStartPoint;
    private BoundsConfig? _dragStartBounds;
    private SKRect _windowRect;
    private SKRect _contentRect;
    private float _previewScale = 1f;

    public event Action<MarkupEditorControl?, bool>? SelectionChanged;
    public event Action? DocumentChanged;

    public EditorPreviewCanvas(IVisualContext context, string id) : base(context, id) {
        Margin = new Thickness(0);
        Padding = new Thickness(12);
    }

    public void LoadDocument(MarkupEditorDocument document, MarkupEditorControl? selectedControl, bool frameSelected) {
        _document = document;
        _selectedControl = selectedControl;
        _frameSelected = frameSelected;
        Invalidate();
    }

    public override bool CanFocus => true;

    public override void Measure(float dpi) {
        DesiredSize = new Size((int)(980 * dpi), (int)(760 * dpi));
    }

    protected override void OnDraw(IKxCanvas canvas) {
        var skCanvas = canvas.As<SKCanvas>();
        if (skCanvas is null)
            return;

        var rect = LayoutRect;
        skCanvas.DrawRect(rect.Left, rect.Top, rect.Width, rect.Height, _canvasPaint);

        if (_document is null)
            return;

        BuildPreviewGeometry();

        _windowPaint.Color = ParseColor(_document.FrameDefault.BackgroundColor, new SKColor(32, 36, 42));
        _titleBarPaint.Color = ParseColor(_document.FrameDefault.TitleBarColor, new SKColor(40, 46, 54));
        _borderPaint.Color = ParseColor(_document.FrameDefault.BorderColor, new SKColor(88, 98, 114));

        skCanvas.DrawRoundRect(_windowRect, 18, 18, _windowPaint);
        skCanvas.DrawRoundRect(_windowRect, 18, 18, _borderPaint);

        float titleHeight = GetTitleBarHeight();
        var titleRect = new SKRect(_windowRect.Left, _windowRect.Top, _windowRect.Right, _windowRect.Top + titleHeight);
        skCanvas.DrawRoundRect(titleRect, 18, 18, _titleBarPaint);
        skCanvas.DrawText(_document.FrameDefault.Title ?? "Preview", _windowRect.Left + 18, _windowRect.Top + 24, SKTextAlign.Left, _titleFont, _textPaint);

        DrawCloseButton(skCanvas, _document.Frame, _windowRect, _previewScale);


        _hits.Clear();
        _hits.Add(new PreviewHit(null, _windowRect, SKRect.Empty));
        DrawControls(skCanvas, _document.Controls.Where(control => IsFrameLayer(control.Layer)), _windowRect.Left, _windowRect.Top, 0);
        DrawControls(skCanvas, _document.Controls.Where(control => IsContentLayer(control.Layer)), _contentRect.Left, _contentRect.Top, 0);
        DrawControls(skCanvas, _document.Controls.Where(control => IsOverlayLayer(control.Layer)), _windowRect.Left, _windowRect.Top, 0);
    }

    public override bool OnMouseDown(Point p) {
        if (_document is null || !_windowRect.Contains(p.X, p.Y))
            return false;

        Context.UIElementManager.SetFocus(this);

        var hit = HitTest(p);
        if (hit is null) {
            SelectFrame();
            return true;
        }

        if (hit.Control is null) {
            SelectFrame();
            return true;
        }

        _selectedControl = hit.Control;
        _frameSelected = false;
        SelectionChanged?.Invoke(_selectedControl, false);

        if (hit.ResizeHandle.Contains(p.X, p.Y)) {
            _dragMode = DragMode.Resize;
        } else {
            _dragMode = DragMode.Move;
        }

        _dragStartPoint = p;
        _dragStartBounds = new BoundsConfig {
            X = _selectedControl.Bounds.X,
            Y = _selectedControl.Bounds.Y,
            Width = _selectedControl.Bounds.Width,
            Height = _selectedControl.Bounds.Height
        };

        Invalidate();
        return true;
    }

    public override bool OnMouseMove(Point p) {
        if (_document is null || _selectedControl is null || _dragMode == DragMode.None || _dragStartBounds is null)
            return false;

        int dx = (int)Math.Round((p.X - _dragStartPoint.X) / _previewScale);
        int dy = (int)Math.Round((p.Y - _dragStartPoint.Y) / _previewScale);

        if (_dragMode == DragMode.Move) {
            _selectedControl.Bounds.X = _dragStartBounds.X + dx;
            _selectedControl.Bounds.Y = _dragStartBounds.Y + dy;
        } else if (_dragMode == DragMode.Resize) {
            _selectedControl.Bounds.Width = Math.Max(20, _dragStartBounds.Width + dx);
            _selectedControl.Bounds.Height = Math.Max(20, _dragStartBounds.Height + dy);
        }

        ClampControlToAllowedArea(_selectedControl);
        DocumentChanged?.Invoke();
        Invalidate();
        return true;
    }

    public override bool OnMouseUp(Point p) {
        if (_dragMode == DragMode.None)
            return false;

        _dragMode = DragMode.None;
        _dragStartBounds = null;
        DocumentChanged?.Invoke();
        Invalidate();
        return true;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _canvasPaint.Dispose();
            _windowPaint.Dispose();
            _titleBarPaint.Dispose();
            _borderPaint.Dispose();
            _controlPaint.Dispose();
            _selectionPaint.Dispose();
            _handlePaint.Dispose();
            _textPaint.Dispose();
            _titleFont.Dispose();
            _bodyFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildPreviewGeometry() {
        float availableWidth = Math.Max(200, ContentRect.Width - 24);
        float availableHeight = Math.Max(180, ContentRect.Height - 24);
        _previewScale = Math.Min(availableWidth / VirtualWidth, availableHeight / VirtualHeight);
        _previewScale = Math.Max(0.2f, _previewScale);

        float width = VirtualWidth * _previewScale;
        float height = VirtualHeight * _previewScale;
        float x = ContentRect.Left + (ContentRect.Width - width) / 2f;
        float y = ContentRect.Top + (ContentRect.Height - height) / 2f;

        _windowRect = new SKRect(x, y, x + width, y + height);

        float titleHeight = GetTitleBarHeight();
        float padding = GetContentPadding();
        _contentRect = new SKRect(
            _windowRect.Left + padding,
            _windowRect.Top + titleHeight + padding,
            _windowRect.Right - padding,
            _windowRect.Bottom - padding);
    }

    private void DrawControls(SKCanvas canvas, IEnumerable<MarkupEditorControl> controls, float originX, float originY, int depth) {
        foreach (var control in controls) {
            var rect = CreatePreviewRect(control, originX, originY);
            var resizeHandle = new SKRect(rect.Right - 10, rect.Bottom - 10, rect.Right, rect.Bottom);

            _hits.Add(new PreviewHit(control, rect, resizeHandle));

            _controlPaint.Color = GetControlColor(control);
            canvas.DrawRoundRect(rect, 10, 10, _controlPaint);

            using var stroke = new SKPaint {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                Color = new SKColor(220, 228, 238, 110)
            };
            canvas.DrawRoundRect(rect, 10, 10, stroke);

            var text = string.IsNullOrWhiteSpace(control.Text)
                ? $"{control.Type} [{control.Id}]"
                : $"{control.Type}: {control.Text}";
            canvas.DrawText(text, rect.Left + 10, rect.Top + 20, SKTextAlign.Left, _bodyFont, _textPaint);

            if (ReferenceEquals(control, _selectedControl) && !_frameSelected) {
                canvas.DrawRoundRect(rect, 10, 10, _selectionPaint);
                canvas.DrawRect(resizeHandle, _handlePaint);
            }

            if (control.Children.Count > 0)
                DrawControls(canvas, control.Children, rect.Left, rect.Top, depth + 1);
        }

        if (_frameSelected)
            canvas.DrawRoundRect(_windowRect, 18, 18, _selectionPaint);
    }

    private PreviewHit? HitTest(Point point) {
        for (var index = _hits.Count - 1; index >= 0; index--) {
            var hit = _hits[index];
            if (hit.Rect.Contains(point.X, point.Y))
                return hit;
        }

        return null;
    }

    private void SelectFrame() {
        _selectedControl = null;
        _frameSelected = true;
        SelectionChanged?.Invoke(null, true);
        Invalidate();
    }

    private SKRect CreatePreviewRect(MarkupEditorControl control, float originX, float originY) {
        return new SKRect(
            originX + control.Bounds.X * _previewScale,
            originY + control.Bounds.Y * _previewScale,
            originX + (control.Bounds.X + control.Bounds.Width) * _previewScale,
            originY + (control.Bounds.Y + control.Bounds.Height) * _previewScale);
    }

    private static bool IsFrameLayer(string? layer) {
        return string.Equals(layer, "Frame", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContentLayer(string? layer) {
        return string.IsNullOrWhiteSpace(layer) || string.Equals(layer, "Content", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOverlayLayer(string? layer) {
        return string.Equals(layer, "Overlay", StringComparison.OrdinalIgnoreCase);
    }

    public void ClampControlToAllowedArea(MarkupEditorControl control) {
        if (_document is null)
            return;

        BuildPreviewGeometry();
        var limit = GetAllowedVirtualBounds(control);

        control.Bounds.Width = Math.Clamp(control.Bounds.Width, 20, Math.Max(20, limit.Width));
        control.Bounds.Height = Math.Clamp(control.Bounds.Height, 20, Math.Max(20, limit.Height));
        control.Bounds.X = Math.Clamp(control.Bounds.X, limit.X, Math.Max(limit.X, limit.X + limit.Width - control.Bounds.Width));
        control.Bounds.Y = Math.Clamp(control.Bounds.Y, limit.Y, Math.Max(limit.Y, limit.Y + limit.Height - control.Bounds.Height));
    }

    private BoundsConfig GetAllowedVirtualBounds(MarkupEditorControl control) {
        if (_document is null)
            return new BoundsConfig { X = 0, Y = 0, Width = VirtualWidth, Height = VirtualHeight };

        var parent = FindParent(_document.Controls, control);
        if (parent is not null) {
            return new BoundsConfig {
                X = 0,
                Y = 0,
                Width = Math.Max(20, parent.Bounds.Width),
                Height = Math.Max(20, parent.Bounds.Height)
            };
        }

        if (IsContentLayer(control.Layer)) {
            return new BoundsConfig {
                X = 0,
                Y = 0,
                Width = Math.Max(20, (int)Math.Round(_contentRect.Width / _previewScale)),
                Height = Math.Max(20, (int)Math.Round(_contentRect.Height / _previewScale))
            };
        }

        return new BoundsConfig {
            X = 0,
            Y = 0,
            Width = VirtualWidth,
            Height = VirtualHeight
        };
    }

    private static MarkupEditorControl? FindParent(IEnumerable<MarkupEditorControl> controls, MarkupEditorControl target) {
        foreach (var control in controls) {
            if (control.Children.Any(child => ReferenceEquals(child, target)))
                return control;

            var parent = FindParent(control.Children, target);
            if (parent is not null)
                return parent;
        }

        return null;
    }

    private float GetTitleBarHeight() {
        return Math.Max(32, _document?.FrameDefault.TitleBarHeight ?? 36) * _previewScale;
    }

    private float GetContentPadding() {
        return Math.Max(8, _document?.FrameDefault.ContentPadding ?? 10) * _previewScale;
    }

    private static SKRect GetCloseButtonRectFromConfig(FrameConfig frame, SKSize size) {
        float width = Math.Max(0f, size.Width);
        float height = Math.Max(0f, size.Height);

        float border = Math.Max(0f, frame.Default.BorderThickness);
        float titleBarHeight = Math.Min(Math.Max(0f, frame.Default.TitleBarHeight), Math.Max(0f, height - border * 2));
        var titleBarRect = new SKRect(border, border, Math.Max(border, width - border), border + titleBarHeight);

        float margin = Math.Max(0f, frame.Default.CloseButtonMargin);
        float availableHeight = Math.Max(0f, titleBarRect.Height - margin * 2);
        float buttonSize = Math.Min(Math.Max(0f, frame.Default.CloseButtonSize), availableHeight);
        float top = titleBarRect.Top + Math.Max(0f, (titleBarRect.Height - buttonSize) / 2f);
        float right = titleBarRect.Right - margin;

        return new SKRect(
            Math.Max(titleBarRect.Left, right - buttonSize),
            top,
            right,
            top + buttonSize);
    }

    private void DrawCloseButton(SKCanvas canvas, FrameConfig frameConfig, SKRect windowRect, float previewScale) {
        var nativeSize = new SKSize(windowRect.Width / previewScale, windowRect.Height / previewScale);
        var closeRectNative = GetCloseButtonRectFromConfig(frameConfig, nativeSize);

        var closeRect = new SKRect(
            windowRect.Left + closeRectNative.Left * previewScale,
            windowRect.Top + closeRectNative.Top * previewScale,
            windowRect.Left + closeRectNative.Right * previewScale,
            windowRect.Top + closeRectNative.Bottom * previewScale);

        using var closeButtonPaint = new SKPaint {
            Color = ParseColor(frameConfig.Default.CloseButtonColor, new SKColor(210, 84, 84)),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        float radius = Math.Min(closeRect.Width, closeRect.Height) / 4f;
        canvas.DrawRoundRect(closeRect, radius, radius, closeButtonPaint);

        using var xPaint = new SKPaint {
            IsAntialias = true,
            Color = ParseColor(frameConfig.Default.CloseButtonForegroundColor, SKColors.White),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1.2f, Math.Min(closeRect.Width, closeRect.Height) * 0.12f),
            StrokeCap = SKStrokeCap.Round
        };

        float pad = Math.Min(closeRect.Width, closeRect.Height) * 0.28f;
        float x1 = closeRect.Left + pad;
        float y1 = closeRect.Top + pad;
        float x2 = closeRect.Right - pad;
        float y2 = closeRect.Bottom - pad;

        canvas.DrawLine(x1, y1, x2, y2, xPaint);
        canvas.DrawLine(x1, y2, x2, y1, xPaint);
    }

    private static SKColor ParseColor(string? value, SKColor fallback) {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        try {
            return SKColor.Parse(value);
        }
        catch {
            return fallback;
        }
    }

    private static SKColor GetControlColor(MarkupEditorControl control) {
        if (!string.IsNullOrWhiteSpace(control.Color)) {
            try {
                return SKColor.Parse(control.Color);
            }
            catch {
            }
        }

        return control.Type.ToLowerInvariant() switch {
            "button" => new SKColor(63, 122, 214),
            "textbox" => new SKColor(62, 74, 90),
            "listbox" => new SKColor(66, 86, 104),
            "stackpanel" => new SKColor(60, 98, 106),
            "grid" => new SKColor(74, 86, 126),
            _ => new SKColor(98, 116, 148)
        };
    }

    private sealed record PreviewHit(MarkupEditorControl? Control, SKRect Rect, SKRect ResizeHandle);
}
