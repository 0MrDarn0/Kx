// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Core.Extensions;
using KUpdater.UI.Layout;
using SkiaSharp;

namespace KUpdater.UI.Elements.Panel;

public class Grid(WindowContext ctx, string id) : Panel(ctx, id) {
    public List<RowDefinition> Rows { get; } = [];
    public List<ColumnDefinition> Columns { get; } = [];

    public override void Measure(float dpi) {
        int maxWidth = 0;
        int maxHeight = 0;

        foreach (var child in Children) {
            child.Measure(dpi);
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        DesiredSize = new Size(maxWidth, maxHeight)
            .AddPadding(Padding, dpi)
            .AddMargin(Margin, dpi);
    }


    public override void Arrange(Rectangle finalRect, float dpi) {
        base.Arrange(finalRect, dpi);
        finalRect = finalRect.ApplyMargin(Margin, dpi);
        LayoutRect = finalRect;
        _bounds.Value = finalRect;

        float totalStarWidth = 0;
        float totalStarHeight = 0;

        float fixedWidth = 0;
        float fixedHeight = 0;

        foreach (var col in Columns) {
            if (col.Width.UnitType == GridUnitType.Pixel)
                fixedWidth += col.Width.Value * dpi;
            else if (col.Width.UnitType == GridUnitType.Auto)
                fixedWidth += 0;
            else
                totalStarWidth += col.Width.Value;
        }

        foreach (var row in Rows) {
            if (row.Height.UnitType == GridUnitType.Pixel)
                fixedHeight += row.Height.Value * dpi;
            else if (row.Height.UnitType == GridUnitType.Auto)
                fixedHeight += 0;
            else
                totalStarHeight += row.Height.Value;
        }

        float remainingWidth = finalRect.Width - fixedWidth;
        float remainingHeight = finalRect.Height - fixedHeight;

        foreach (var col in Columns) {
            if (col.Width.UnitType == GridUnitType.Pixel)
                col.ActualWidth = col.Width.Value * dpi;
            else if (col.Width.UnitType == GridUnitType.Auto)
                col.ActualWidth = remainingWidth / Columns.Count;
            else
                col.ActualWidth = (col.Width.Value / totalStarWidth) * remainingWidth;
        }

        foreach (var row in Rows) {
            if (row.Height.UnitType == GridUnitType.Pixel)
                row.ActualHeight = row.Height.Value * dpi;
            else if (row.Height.UnitType == GridUnitType.Auto)
                row.ActualHeight = remainingHeight / Rows.Count;
            else
                row.ActualHeight = (row.Height.Value / totalStarHeight) * remainingHeight;
        }

        foreach (var child in Children) {
            float x = finalRect.X;
            float y = finalRect.Y;

            for (int c = 0; c < child.GridColumn; c++)
                x += Columns[c].ActualWidth;

            for (int r = 0; r < child.GridRow; r++)
                y += Rows[r].ActualHeight;

            float width = 0;
            float height = 0;

            for (int c = 0; c < child.GridColumnSpan; c++)
                width += Columns[child.GridColumn + c].ActualWidth;

            for (int r = 0; r < child.GridRowSpan; r++)
                height += Rows[child.GridRow + r].ActualHeight;

            child.Arrange(new Rectangle((int)x, (int)y, (int)width, (int)height), dpi);
        }
    }

    protected override void DrawDebugOverlay(SKCanvas canvas) {
        base.DrawDebugOverlay(canvas);

        using var paint = new SKPaint {
            Color = new SKColor(255, 255, 255, 190),
            Style = SKPaintStyle.Fill,
            StrokeWidth = 5
        };

        float x = LayoutRect.Left;
        foreach (var col in Columns) {
            x += col.ActualWidth;
            canvas.DrawLine(x, LayoutRect.Top, x, LayoutRect.Bottom, paint);
        }

        float y = LayoutRect.Top;
        foreach (var row in Rows) {
            y += row.ActualHeight;
            canvas.DrawLine(LayoutRect.Left, y, LayoutRect.Right, y, paint);
        }
    }

}
