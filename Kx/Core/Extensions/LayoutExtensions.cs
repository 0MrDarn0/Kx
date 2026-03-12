// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.UI.Layout;

namespace Kx.Core.Extensions;

public static class LayoutExtensions {
    public static Size AddMargin(this Size size, Thickness margin, float dpi) {
        return new Size(
            size.Width + (int)(margin.Horizontal * dpi),
            size.Height + (int)(margin.Vertical * dpi)
        );
    }

    public static Size AddPadding(this Size size, Thickness padding, float dpi) {
        return new Size(
            size.Width + (int)(padding.Horizontal * dpi),
            size.Height + (int)(padding.Vertical * dpi)
        );
    }

    public static Rectangle ApplyMargin(this Rectangle rect, Thickness margin, float dpi) {
        return new Rectangle(
            rect.X + (int)(margin.Left * dpi),
            rect.Y + (int)(margin.Top * dpi),
            rect.Width - (int)(margin.Horizontal * dpi),
            rect.Height - (int)(margin.Vertical * dpi)
        );
    }

    public static Rectangle ApplyPadding(this Rectangle rect, Thickness padding, float dpi) {
        return new Rectangle(
            rect.X + (int)(padding.Left * dpi),
            rect.Y + (int)(padding.Top * dpi),
            rect.Width - (int)(padding.Horizontal * dpi),
            rect.Height - (int)(padding.Vertical * dpi)
        );
    }

}
