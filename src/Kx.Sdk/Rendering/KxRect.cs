// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Rendering;

/// <summary>
/// Represents a renderer-neutral rectangle defined by left, top, right, and bottom coordinates.
/// </summary>
/// <param name="Left">The left coordinate.</param>
/// <param name="Top">The top coordinate.</param>
/// <param name="Right">The right coordinate.</param>
/// <param name="Bottom">The bottom coordinate.</param>
public readonly record struct KxRect(float Left, float Top, float Right, float Bottom) {
    /// <summary>
    /// Gets the rectangle width.
    /// </summary>
    public float Width => Right - Left;

    /// <summary>
    /// Gets the rectangle height.
    /// </summary>
    public float Height => Bottom - Top;

    /// <summary>
    /// Gets the horizontal center coordinate.
    /// </summary>
    public float MidX => Left + (Width / 2f);

    /// <summary>
    /// Gets the vertical center coordinate.
    /// </summary>
    public float MidY => Top + (Height / 2f);

    /// <summary>
    /// Gets whether the rectangle has no positive area.
    /// </summary>
    public bool IsEmpty => Width <= 0f || Height <= 0f;
}
