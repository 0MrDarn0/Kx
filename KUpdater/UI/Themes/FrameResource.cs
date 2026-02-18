// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Utility;
using SkiaSharp;

namespace KUpdater.UI.Themes;

public sealed class FrameResource : IDisposable {
    // Original Properties
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
    public bool UseFillColor { get; private set; }

    public int TopWidthOffset { get; private set; }
    public int BottomWidthOffset { get; private set; }
    public int LeftHeightOffset { get; private set; }
    public int RightHeightOffset { get; private set; }
    public int FillPosOffset { get; private set; }
    public int FillWidthOffset { get; private set; }
    public int FillHeightOffset { get; private set; }

    // NEW: Auto‑Generation Settings
    public bool AutoGenerateMissing { get; set; } = true;
    public SKColor PlaceholderColor { get; set; } = new SKColor(255, 0, 0, 80);
    public int DefaultCornerSize { get; set; } = 64;
    public int DefaultEdgeThickness { get; set; } = 48;
    public float DpiScale { get; private set; } = 1f;

    private bool _disposed;

    private FrameResource() { }

    public static FrameResource FromConfig(FrameConfig cfg, IResourceProvider provider, float dpiScale) {
        var fr = new FrameResource {
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
            UseFillColor = cfg.UseFillColor,

            TopWidthOffset = cfg.TopWidthOffset,
            BottomWidthOffset = cfg.BottomWidthOffset,
            LeftHeightOffset = cfg.LeftHeightOffset,
            RightHeightOffset = cfg.RightHeightOffset,
            FillPosOffset = cfg.FillPosOffset,
            FillWidthOffset = cfg.FillWidthOffset,
            FillHeightOffset = cfg.FillHeightOffset,
            DpiScale = dpiScale,
        };

        fr.ApplyDpiScaling();

        return fr;
    }


    // ---------------------------------------------------------
    // 1) Placeholder‑Bitmap erzeugen
    // ---------------------------------------------------------
    private SKBitmap CreatePlaceholder(int width, int height) {
        width = (int)(width * DpiScale);
        height = (int)(height * DpiScale);

        var bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);

        canvas.Clear(SKColors.Transparent);

        using var p = new SKPaint {
            Color = PlaceholderColor,
            StrokeWidth = 2 * DpiScale,
            IsStroke = true,
            IsAntialias = true
        };

        canvas.DrawLine(0, 0, width, height, p);
        canvas.DrawLine(width, 0, 0, height, p);

        return bmp;
    }

    // ---------------------------------------------------------
    // 2) Fehlende Teile automatisch generieren
    // ---------------------------------------------------------
    public void AutoGenerateMissingParts(int windowWidth, int windowHeight) {
        if (!AutoGenerateMissing)
            return;

        int corner = (int)(DefaultCornerSize * DpiScale);
        int edge   = (int)(DefaultEdgeThickness * DpiScale);

        TopLeft ??= CreatePlaceholder(corner, corner);
        TopRight ??= CreatePlaceholder(corner, corner);
        BottomLeft ??= CreatePlaceholder(corner, corner);
        BottomRight ??= CreatePlaceholder(corner, corner);

        TopCenter ??= CreatePlaceholder(Math.Max(1, windowWidth - corner * 2), edge);
        BottomCenter ??= CreatePlaceholder(Math.Max(1, windowWidth - corner * 2), edge);
        LeftCenter ??= CreatePlaceholder(edge, Math.Max(1, windowHeight - corner * 2));
        RightCenter ??= CreatePlaceholder(edge, Math.Max(1, windowHeight - corner * 2));

        FillBitmap ??= CreatePlaceholder(
            Math.Max(1, (int)((windowWidth - edge * 2) * DpiScale)),
            Math.Max(1, (int)((windowHeight - edge * 2) * DpiScale))
        );
    }

    public void ApplyDpiScaling() {
        if (DpiScale <= 1.01f)
            return;

        SKBitmap? ScaleBitmap(SKBitmap? bmp) {
            if (bmp == null)
                return null;

            int newW = (int)(bmp.Width * DpiScale);
            int newH = (int)(bmp.Height * DpiScale);

            var scaled = new SKBitmap(newW, newH, bmp.ColorType, bmp.AlphaType);
            using var canvas = new SKCanvas(scaled);
            canvas.DrawBitmap(bmp, new SKRect(0, 0, newW, newH));
            return scaled;
        }

        TopLeft = ScaleBitmap(TopLeft);
        TopCenter = ScaleBitmap(TopCenter);
        TopRight = ScaleBitmap(TopRight);
        RightCenter = ScaleBitmap(RightCenter);
        BottomRight = ScaleBitmap(BottomRight);
        BottomCenter = ScaleBitmap(BottomCenter);
        BottomLeft = ScaleBitmap(BottomLeft);
        LeftCenter = ScaleBitmap(LeftCenter);
        FillBitmap = ScaleBitmap(FillBitmap);

        // Offsets ebenfalls skalieren
        TopWidthOffset = (int)(TopWidthOffset * DpiScale);
        BottomWidthOffset = (int)(BottomWidthOffset * DpiScale);
        LeftHeightOffset = (int)(LeftHeightOffset * DpiScale);
        RightHeightOffset = (int)(RightHeightOffset * DpiScale);
        FillPosOffset = (int)(FillPosOffset * DpiScale);
        FillWidthOffset = (int)(FillWidthOffset * DpiScale);
        FillHeightOffset = (int)(FillHeightOffset * DpiScale);
    }

    // ---------------------------------------------------------
    // Dispose
    // ---------------------------------------------------------
    public void Dispose() {
        if (_disposed)
            return;

        TopLeft?.Dispose();
        TopCenter?.Dispose();
        TopRight?.Dispose();
        RightCenter?.Dispose();
        BottomRight?.Dispose();
        BottomCenter?.Dispose();
        BottomLeft?.Dispose();
        LeftCenter?.Dispose();
        FillBitmap?.Dispose();

        _disposed = true;
    }
}
