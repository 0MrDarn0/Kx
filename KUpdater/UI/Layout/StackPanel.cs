// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;

namespace KUpdater.UI.Layout;

public class StackPanel : Panel {
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public float Spacing { get; set; } = 4f;

    public StackPanel(WindowContext ctx, string id, Func<Rectangle> boundsFunc)
        : base(ctx, id, boundsFunc) { }

    public override void Measure(float dpiScale) {
        float totalWidth = 0;
        float totalHeight = 0;

        foreach (var child in Children) {
            child.Measure(dpiScale);

            if (Orientation == Orientation.Vertical) {
                totalHeight += child.DesiredSize.Height + (int)(Spacing * dpiScale);
                totalWidth = Math.Max(totalWidth, child.DesiredSize.Width);
            } else {
                totalWidth += child.DesiredSize.Width + (int)(Spacing * dpiScale);
                totalHeight = Math.Max(totalHeight, child.DesiredSize.Height);
            }
        }
        DesiredSize = new Size((int)totalWidth, (int)totalHeight);
    }

    public override void Arrange(Rectangle finalRect, float dpiScale) {
        LayoutRect = finalRect;

        int x = finalRect.X;
        int y = finalRect.Y;

        foreach (var child in Children) {
            if (Orientation == Orientation.Vertical) {
                child.Arrange(new Rectangle(x, y, LayoutRect.Width, child.DesiredSize.Height), dpiScale);
                y += child.DesiredSize.Height + (int)(Spacing * dpiScale);
            } else {
                child.Arrange(new Rectangle(x, y, child.DesiredSize.Width, finalRect.Height), dpiScale);
                x += child.DesiredSize.Width + (int)(Spacing * dpiScale);
            }
        }
    }
}
