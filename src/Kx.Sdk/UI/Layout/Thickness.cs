// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Layout;

public struct Thickness(float left, float top, float right, float bottom) {
    public float Left = left;
    public float Top = top;
    public float Right = right;
    public float Bottom = bottom;

    public Thickness(float uniform)
        : this(uniform, uniform, uniform, uniform) {
    }

    public Thickness(float horizontal, float vertical)
        : this(horizontal, vertical, horizontal, vertical) {
    }

    public readonly float Horizontal => Left + Right;
    public readonly float Vertical => Top + Bottom;
}
