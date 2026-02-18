// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Utility;
using SkiaSharp;

namespace KUpdater.UI.Themes;

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
