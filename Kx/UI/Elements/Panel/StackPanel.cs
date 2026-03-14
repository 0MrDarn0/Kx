// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Core.Extensions;

namespace Kx.UI.Elements.Panel;

public class StackPanel(IVisualContext ctx, string id) : Panel(ctx, id) {
    public Layout.Orientation Orientation { get; set; } = Layout.Orientation.Vertical;
    public float Spacing { get; set; } = 4f;

    public override void Measure(float dpi) {
        float totalWidth = 0;
        float totalHeight = 0;

        foreach (var child in Children) {
            child.Measure(dpi);

            if (Orientation == Layout.Orientation.Vertical) {
                totalHeight += child.DesiredSize.Height + (int)(Spacing * dpi);
                totalWidth = Math.Max(totalWidth, child.DesiredSize.Width);
            } else {
                totalWidth += child.DesiredSize.Width + (int)(Spacing * dpi);
                totalHeight = Math.Max(totalHeight, child.DesiredSize.Height);
            }
        }

        var size = new Size((int)totalWidth, (int)totalHeight)
        .AddPadding(Padding, dpi)
        .AddMargin(Margin, dpi);

        DesiredSize = size;
    }


    public override void Arrange(Rectangle finalRect, float dpi) {
        finalRect = finalRect.ApplyMargin(Margin, dpi);

        LayoutRect = finalRect;
        _bounds.Value = finalRect;

        int x = finalRect.X;
        int y = finalRect.Y;

        foreach (var child in Children) {
            if (Orientation == Layout.Orientation.Vertical) {
                var childRect = new Rectangle(
                x,
                y,
                finalRect.Width,
                child.DesiredSize.Height
            );

                child.Arrange(childRect, dpi);
                y += child.DesiredSize.Height + (int)(Spacing * dpi);
            } else {
                var childRect = new Rectangle(
                x,
                y,
                child.DesiredSize.Width,
                finalRect.Height
            );

                child.Arrange(childRect, dpi);
                x += child.DesiredSize.Width + (int)(Spacing * dpi);
            }
        }
    }

}
