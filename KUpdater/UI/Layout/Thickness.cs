// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Layout;

public struct Thickness {
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;

    public Thickness(float uniform)
        : this(uniform, uniform, uniform, uniform) { }

    public Thickness(float horizontal, float vertical)
        : this(horizontal, vertical, horizontal, vertical) { }

    public Thickness(float left, float top, float right, float bottom) {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
}
