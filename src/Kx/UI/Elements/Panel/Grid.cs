// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.UI;
using Kx.UI.Layout;

namespace Kx.UI.Elements.Panel;

public class Grid(IVisualContext ctx, string id) : Panel(ctx, id) {
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

        // Ensure at least one column/row exists to avoid index errors when children assume defaults
        if (Columns.Count == 0)
            Columns.Add(new ColumnDefinition { Width = GridLength.Star(1) });

        if (Rows.Count == 0)
            Rows.Add(new RowDefinition { Height = GridLength.Star(1) });

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

        if (remainingWidth < 0)
            remainingWidth = 0;
        if (remainingHeight < 0)
            remainingHeight = 0;

        // If there are star columns but totalStarWidth is zero (defensive), treat each star as weight 1
        if (totalStarWidth == 0 && Columns.Any(c => c.Width.UnitType == GridUnitType.Star))
            totalStarWidth = Columns.Count(c => c.Width.UnitType == GridUnitType.Star);

        foreach (var col in Columns) {
            if (col.Width.UnitType == GridUnitType.Pixel)
                col.ActualWidth = col.Width.Value * dpi;
            else if (col.Width.UnitType == GridUnitType.Auto)
                col.ActualWidth = remainingWidth / Columns.Count;
            else
                col.ActualWidth = (col.Width.Value / totalStarWidth) * remainingWidth;
        }

        if (totalStarHeight == 0 && Rows.Any(r => r.Height.UnitType == GridUnitType.Star))
            totalStarHeight = Rows.Count(r => r.Height.UnitType == GridUnitType.Star);

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

}
