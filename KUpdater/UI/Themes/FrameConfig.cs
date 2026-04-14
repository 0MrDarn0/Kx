// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Themes;

public class FrameConfig {
    public string TopLeft { get; set; } = "KalOnline:Frame:top_left.png";
    public string TopCenter { get; set; } = "KalOnline:Frame:top_center.png";
    public string TopRight { get; set; } = "KalOnline:Frame:top_right.png";
    public string RightCenter { get; set; } = "KalOnline:Frame:right_center.png";
    public string BottomRight { get; set; } = "KalOnline:Frame:bottom_right.png";
    public string BottomCenter { get; set; } = "KalOnline:Frame:bottom_center.png";
    public string BottomLeft { get; set; } = "KalOnline:Frame:bottom_left.png";
    public string LeftCenter { get; set; } = "KalOnline:Frame:left_center.png";
    public string FillBitmap { get; set; } = "KalOnline:Frame:fill_bitmap.bmp";
    public string FillColor { get; set; } = "#101010";
    public bool UseFillColor { get; set; } = false;
    public int TopWidthOffset { get; set; } = 7;
    public int BottomWidthOffset { get; set; } = 15;
    public int LeftHeightOffset { get; set; } = 5;
    public int RightHeightOffset { get; set; } = 5;
    public int FillPosOffset { get; set; } = 5;
    public int FillWidthOffset { get; set; } = 12;
    public int FillHeightOffset { get; set; } = 9;
}
