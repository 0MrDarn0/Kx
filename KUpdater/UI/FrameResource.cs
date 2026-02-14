// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Utility;
using SkiaSharp;

namespace KUpdater.UI;

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

    public int TopWidthOffset { get; set; } = 7;
    public int BottomWidthOffset { get; set; } = 15;
    public int LeftHeightOffset { get; set; } = 5;
    public int RightHeightOffset { get; set; } = 5;
    public int FillPosOffset { get; set; } = 5;
    public int FillWidthOffset { get; set; } = 12;
    public int FillHeightOffset { get; set; } = 9;
}
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

public static class FrameLoader {
    public static FrameResources Load(FrameConfig cfg, IResourceProvider provider) {
        return new FrameResources {
            TopLeft = provider.TryGetSkiaBitmap(cfg.TopLeft),
            TopCenter = provider.TryGetSkiaBitmap(cfg.TopCenter),
            TopRight = provider.TryGetSkiaBitmap(cfg.TopRight),
            RightCenter = provider.TryGetSkiaBitmap(cfg.RightCenter),
            BottomRight = provider.TryGetSkiaBitmap(cfg.BottomRight),
            BottomCenter = provider.TryGetSkiaBitmap(cfg.BottomCenter),
            BottomLeft = provider.TryGetSkiaBitmap(cfg.BottomLeft),
            LeftCenter = provider.TryGetSkiaBitmap(cfg.LeftCenter),
            FillBitmap = provider.TryGetSkiaBitmap(cfg.FillBitmap),
            FillColor = SKColor.Parse(cfg.FillColor),

            TopWidthOffset = cfg.TopWidthOffset,
            BottomWidthOffset = cfg.BottomWidthOffset,
            LeftHeightOffset = cfg.LeftHeightOffset,
            RightHeightOffset = cfg.RightHeightOffset,
            FillPosOffset = cfg.FillPosOffset,
            FillWidthOffset = cfg.FillWidthOffset,
            FillHeightOffset = cfg.FillHeightOffset
        };
    }
}
