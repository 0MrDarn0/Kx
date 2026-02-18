// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace KUpdater.UI.Themes;

public class FrameResources {
    public SKBitmap? TopLeft { get; init; }
    public SKBitmap? TopCenter { get; init; }
    public SKBitmap? TopRight { get; init; }
    public SKBitmap? RightCenter { get; init; }
    public SKBitmap? BottomRight { get; init; }
    public SKBitmap? BottomCenter { get; init; }
    public SKBitmap? BottomLeft { get; init; }
    public SKBitmap? LeftCenter { get; init; }
    public SKBitmap? FillBitmap { get; init; }
    public SKColor FillColor { get; init; }
    public int TopWidthOffset { get; init; }
    public int BottomWidthOffset { get; init; }
    public int LeftHeightOffset { get; init; }
    public int RightHeightOffset { get; init; }
    public int FillPosOffset { get; init; }
    public int FillWidthOffset { get; init; }
    public int FillHeightOffset { get; init; }
}
