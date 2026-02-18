// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Utility;
using SkiaSharp;

namespace KUpdater.UI.Themes;

public sealed class FrameResource : IDisposable {
    public SKBitmap? TopLeft { get; private set; }
    public SKBitmap? TopCenter { get; private set; }
    public SKBitmap? TopRight { get; private set; }
    public SKBitmap? RightCenter { get; private set; }
    public SKBitmap? BottomRight { get; private set; }
    public SKBitmap? BottomCenter { get; private set; }
    public SKBitmap? BottomLeft { get; private set; }
    public SKBitmap? LeftCenter { get; private set; }
    public SKBitmap? FillBitmap { get; private set; }
    public SKColor FillColor { get; private set; }
    public int TopWidthOffset { get; private set; }
    public int BottomWidthOffset { get; private set; }
    public int LeftHeightOffset { get; private set; }
    public int RightHeightOffset { get; private set; }
    public int FillPosOffset { get; private set; }
    public int FillWidthOffset { get; private set; }
    public int FillHeightOffset { get; private set; }

    private bool _disposed;

    private FrameResource() { }

    public static FrameResource FromConfig(FrameConfig cfg, IResourceProvider provider) {
        return new FrameResource {
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

    public void Dispose() {
        if (_disposed)
            return;
        TopLeft?.Dispose();
        TopLeft = null;
        TopCenter?.Dispose();
        TopCenter = null;
        TopRight?.Dispose();
        TopRight = null;
        RightCenter?.Dispose();
        RightCenter = null;
        BottomRight?.Dispose();
        BottomRight = null;
        BottomCenter?.Dispose();
        BottomCenter = null;
        BottomLeft?.Dispose();
        BottomLeft = null;
        LeftCenter?.Dispose();
        LeftCenter = null;
        FillBitmap?.Dispose();
        FillBitmap = null;
        _disposed = true;
    }
}
