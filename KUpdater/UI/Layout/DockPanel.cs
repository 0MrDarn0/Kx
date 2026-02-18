// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;

namespace KUpdater.UI.Layout;

public class DockPanel : Panel {
    public DockPanel(WindowContext ctx, string id, Func<Rectangle> boundsFunc)
        : base(ctx, id, boundsFunc) { }

    public override void Measure(float dpi) {
        foreach (var child in Children)
            child.Measure(dpi);

        DesiredSize = Bounds.Size;
    }

    public override void Arrange(Rectangle finalRect, float dpi) {
        LayoutRect = finalRect;

        int left = finalRect.Left;
        int top = finalRect.Top;
        int right = finalRect.Right;
        int bottom = finalRect.Bottom;

        foreach (var child in Children) {
            var dock = Dock.Fill;

            if (child is IDockable dockable)
                dock = dockable.Dock;

            var size = child.DesiredSize;

            switch (dock) {
                case Dock.Left:
                child.Arrange(
                    new Rectangle(left, top, size.Width, bottom - top),
                    dpi
                );
                left += size.Width;
                break;

                case Dock.Right:
                child.Arrange(
                    new Rectangle(right - size.Width, top, size.Width, bottom - top),
                    dpi
                );
                right -= size.Width;
                break;

                case Dock.Top:
                child.Arrange(
                    new Rectangle(left, top, right - left, size.Height),
                    dpi
                );
                top += size.Height;
                break;

                case Dock.Bottom:
                child.Arrange(
                    new Rectangle(left, bottom - size.Height, right - left, size.Height),
                    dpi
                );
                bottom -= size.Height;
                break;

                case Dock.Fill:
                child.Arrange(
                    new Rectangle(left, top, right - left, bottom - top),
                    dpi
                );
                break;
            }
        }
    }
}
