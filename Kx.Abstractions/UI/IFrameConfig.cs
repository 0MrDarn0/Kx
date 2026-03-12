// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI;

public interface IFrameConfig {
    string BottomCenter { get; set; }
    string BottomLeft { get; set; }
    string BottomRight { get; set; }
    int BottomWidthOffset { get; set; }
    string FillBitmap { get; set; }
    string FillColor { get; set; }
    int FillHeightOffset { get; set; }
    int FillPosOffset { get; set; }
    int FillWidthOffset { get; set; }
    string LeftCenter { get; set; }
    int LeftHeightOffset { get; set; }
    string RightCenter { get; set; }
    int RightHeightOffset { get; set; }
    string TopCenter { get; set; }
    string TopLeft { get; set; }
    string TopRight { get; set; }
    int TopWidthOffset { get; set; }
    bool UseFillColor { get; set; }
}
