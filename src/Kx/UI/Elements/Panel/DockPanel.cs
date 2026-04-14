// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Layout;
using Kx.Core.Extensions;

namespace Kx.UI.Elements.Panel;

public class DockPanel(IVisualContext ctx, string id) : Panel(ctx, id) {
    public override void Measure(float dpi) {
        int width = 0;
        int height = 0;

        foreach (var child in Children) {
            child.Measure(dpi);
            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        DesiredSize = new Size(width, height)
            .AddPadding(Padding, dpi)
            .AddMargin(Margin, dpi);
    }

    public override void Arrange(Rectangle finalRect, float dpi) {
        finalRect = finalRect.ApplyMargin(Margin, dpi);
        LayoutRect = finalRect;
        _bounds.Value = finalRect;

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
