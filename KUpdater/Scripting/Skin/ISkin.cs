// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace KUpdater.Scripting.Skin;

public interface ISkin {
    void Dispose();
    SkinBackground GetBackground();
    SkinLayout GetLayout();
}

public class SkinBackground {
    public SKBitmap? TopLeft { get; init; }
    public SKBitmap? TopCenter { get; init; }
    public SKBitmap? TopRight { get; init; }
    public SKBitmap? RightCenter { get; init; }
    public SKBitmap? BottomRight { get; init; }
    public SKBitmap? BottomCenter { get; init; }
    public SKBitmap? BottomLeft { get; init; }
    public SKBitmap? LeftCenter { get; init; }
    public SKBitmap? FillBitmap { get; init; }
    public Color FillColor { get; set; } = Color.Black;
}

public class SkinLayout {
    public int TopWidthOffset { get; set; }
    public int BottomWidthOffset { get; set; }
    public int LeftHeightOffset { get; set; }
    public int RightHeightOffset { get; set; }
    public int FillPosOffset { get; set; }
    public int FillWidthOffset { get; set; }
    public int FillHeightOffset { get; set; }
}

/* als record, villeicht in der zukunft
public readonly record struct SkinLayout(
int TopWidthOffset,
int BottomWidthOffset,
int LeftHeightOffset,
int RightHeightOffset,
int FillPosOffset,
int FillWidthOffset,
int FillHeightOffset);
 */
