// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.UI.Layout;

using uOrientation = Kx.UI.Layout.Orientation;

namespace Kx.UI.Elements.Panel;

public sealed class GridSplitter(IVisualContext ctx, string id) : UIElement(ctx, id) {
    private bool _isHovered;
    private bool _isDragging;
    private Point _dragStart;
    private float _firstStartSize;
    private float _secondStartSize;
    private GridLength _firstStartLength;
    private GridLength _secondStartLength;
    private KxColor _trackColor = new(110, 110, 110, 96);
    private KxColor _hoverTrackColor = new(150, 150, 150, 132);
    private KxColor _activeTrackColor = new(214, 214, 214, 176);
    private KxColor _gripColor = new(255, 255, 255, 180);

    public uOrientation Orientation { get; set; } = uOrientation.Vertical;
    public float MinSegmentSize { get; set; } = 48f;
    public int? TargetColumn { get; set; }
    public int? TargetRow { get; set; }
    public KxColor TrackColor {
        get => _trackColor;
        set {
            _trackColor = value;
            Invalidate();
        }
    }

    public KxColor HoverTrackColor {
        get => _hoverTrackColor;
        set {
            _hoverTrackColor = value;
            Invalidate();
        }
    }

    public KxColor ActiveTrackColor {
        get => _activeTrackColor;
        set {
            _activeTrackColor = value;
            Invalidate();
        }
    }

    public KxColor GripColor {
        get => _gripColor;
        set {
            _gripColor = value;
            Invalidate();
        }
    }

    public override bool OnMouseDown(Point p) {
        if (!Bounds.Contains(p))
            return false;

        if (!TryCaptureStartSizes())
            return false;

        _isDragging = true;
        _dragStart = p;
        Context.RequestRender();
        return true;
    }

    public override bool OnMouseMove(Point p) {
        bool isHovered = Bounds.Contains(p);
        if (_isHovered != isHovered) {
            _isHovered = isHovered;
            Context.RequestRender();
        }

        if (!_isDragging)
            return isHovered;

        if (!TryApplyResize(p))
            return false;

        Context.RequestRender();
        return true;
    }

    public override bool OnMouseUp(Point p) {
        bool wasDragging = _isDragging;
        bool wasHovered = _isHovered;

        _isDragging = false;
        _isHovered = Bounds.Contains(p);

        if (wasDragging || wasHovered != _isHovered)
            Context.RequestRender();

        return wasDragging || _isHovered;
    }

    protected override void OnDraw(IKxCanvas canvas) {
        var rect = new KxRect(LayoutRect.Left, LayoutRect.Top, LayoutRect.Right, LayoutRect.Bottom);
        if (rect.IsEmpty)
            return;

        var trackRect = GetTrackRect(rect);

        var trackColor = _isDragging
            ? _activeTrackColor
            : _isHovered
                ? _hoverTrackColor
                : _trackColor;

        canvas.DrawRoundedRect(trackRect.Left, trackRect.Top, trackRect.Right, trackRect.Bottom, 2f * DpiScale, 2f * DpiScale, trackColor);

        const float gripThickness = 2f;
        const float gripLength = 18f;
        const float gripSpacing = 5f;

        if (Orientation == uOrientation.Vertical) {
            float centerX = trackRect.MidX;
            float centerY = trackRect.MidY;
            for (int offset = -1; offset <= 1; offset++) {
                float y = centerY + offset * gripSpacing;
                DrawGrip(canvas, centerX - gripThickness / 2f, y - gripLength / 2f, centerX + gripThickness / 2f, y + gripLength / 2f, gripThickness, _gripColor);
            }

            return;
        }

        float horizontalCenterX = trackRect.MidX;
        float horizontalCenterY = trackRect.MidY;
        for (int offset = -1; offset <= 1; offset++) {
            float x = horizontalCenterX + offset * gripSpacing;
            DrawGrip(canvas, x - gripLength / 2f, horizontalCenterY - gripThickness / 2f, x + gripLength / 2f, horizontalCenterY + gripThickness / 2f, gripThickness, _gripColor);
        }
    }

    private KxRect GetTrackRect(KxRect bounds) {
        float visualThickness = Math.Max(3f * DpiScale, 4f * DpiScale);
        float edgeInset = 1f * DpiScale;

        if (Orientation == uOrientation.Vertical) {
            float width = Math.Min(visualThickness, bounds.Width);
            float left = bounds.MidX - width / 2f;
            return new KxRect(left, bounds.Top + edgeInset, left + width, Math.Max(bounds.Top + edgeInset, bounds.Bottom - edgeInset));
        }

        float height = Math.Min(visualThickness, bounds.Height);
        float top = bounds.MidY - height / 2f;
        return new KxRect(bounds.Left + edgeInset, top, Math.Max(bounds.Left + edgeInset, bounds.Right - edgeInset), top + height);
    }

    private static void DrawGrip(IKxCanvas canvas, float left, float top, float right, float bottom, float radius, KxColor color) {
        canvas.DrawRoundedRect(left, top, right, bottom, radius, radius, color);
    }

    private bool TryCaptureStartSizes() {
        if (!TryGetResizeTargets(out int firstIndex, out int secondIndex))
            return false;

        var grid = Parent as Grid;
        if (grid is null)
            return false;

        if (Orientation == uOrientation.Vertical) {
            _firstStartLength = grid.Columns[firstIndex].Width;
            _secondStartLength = grid.Columns[secondIndex].Width;
            _firstStartSize = grid.Columns[firstIndex].ActualWidth;
            _secondStartSize = grid.Columns[secondIndex].ActualWidth;
        } else {
            _firstStartLength = grid.Rows[firstIndex].Height;
            _secondStartLength = grid.Rows[secondIndex].Height;
            _firstStartSize = grid.Rows[firstIndex].ActualHeight;
            _secondStartSize = grid.Rows[secondIndex].ActualHeight;
        }

        return _firstStartSize > 0f || _secondStartSize > 0f;
    }

    private bool TryApplyResize(Point currentPoint) {
        if (!TryGetResizeTargets(out int firstIndex, out int secondIndex))
            return false;

        var grid = Parent as Grid;
        if (grid is null)
            return false;

        float totalSize = _firstStartSize + _secondStartSize;
        if (totalSize <= 0f)
            return false;

        float delta = Orientation == uOrientation.Vertical
            ? currentPoint.X - _dragStart.X
            : currentPoint.Y - _dragStart.Y;

        float minSize = Math.Max(0f, MinSegmentSize * DpiScale);
        float firstSize = Math.Clamp(_firstStartSize + delta, minSize, Math.Max(minSize, totalSize - minSize));
        float secondSize = totalSize - firstSize;

        if (secondSize < minSize) {
            secondSize = minSize;
            firstSize = Math.Max(minSize, totalSize - secondSize);
        }

        if (Orientation == uOrientation.Vertical) {
            ApplyColumnResize(grid.Columns[firstIndex], grid.Columns[secondIndex], firstSize, secondSize);
        } else {
            ApplyRowResize(grid.Rows[firstIndex], grid.Rows[secondIndex], firstSize, secondSize);
        }

        return true;
    }

    private void ApplyColumnResize(ColumnDefinition firstColumn, ColumnDefinition secondColumn, float firstSize, float secondSize) {
        ArgumentNullException.ThrowIfNull(firstColumn);
        ArgumentNullException.ThrowIfNull(secondColumn);

        firstColumn.Width = CreateResizedLength(_firstStartLength, _secondStartLength, firstSize);
        secondColumn.Width = CreateResizedLength(_secondStartLength, _firstStartLength, secondSize);
    }

    private void ApplyRowResize(RowDefinition firstRow, RowDefinition secondRow, float firstSize, float secondSize) {
        ArgumentNullException.ThrowIfNull(firstRow);
        ArgumentNullException.ThrowIfNull(secondRow);

        firstRow.Height = CreateResizedLength(_firstStartLength, _secondStartLength, firstSize);
        secondRow.Height = CreateResizedLength(_secondStartLength, _firstStartLength, secondSize);
    }

    private GridLength CreateResizedLength(GridLength targetStartLength, GridLength otherStartLength, float targetSize) {
        const float MinimumStarWeight = 0.001f;

        if (targetStartLength.UnitType == GridUnitType.Star && otherStartLength.UnitType == GridUnitType.Star)
            return GridLength.Star(Math.Max(MinimumStarWeight, targetSize));

        if (targetStartLength.UnitType == GridUnitType.Star)
            return targetStartLength.Value > 0f
                ? targetStartLength
                : GridLength.Star(Math.Max(MinimumStarWeight, targetSize));

        if (targetStartLength.UnitType == GridUnitType.Pixel)
            return GridLength.Pixel(targetSize / DpiScale);

        return GridLength.Pixel(targetSize / DpiScale);
    }

    private bool TryGetResizeTargets(out int firstIndex, out int secondIndex) {
        var grid = Parent as Grid;
        if (grid is null) {
            firstIndex = -1;
            secondIndex = -1;
            return false;
        }

        if (Orientation == uOrientation.Vertical) {
            firstIndex = TargetColumn ?? GridColumn - 1;
            secondIndex = TargetColumn.HasValue
                ? firstIndex + 1
                : GridColumn + GridColumnSpan;
            return firstIndex >= 0 && secondIndex < grid.Columns.Count;
        }

        firstIndex = TargetRow ?? GridRow - 1;
        secondIndex = TargetRow.HasValue
            ? firstIndex + 1
            : GridRow + GridRowSpan;
        return firstIndex >= 0 && secondIndex < grid.Rows.Count;
    }
}
