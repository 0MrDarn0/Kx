// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Themes;
using Kx.Utility;

using SkiaSharp;

using uFrameStyle = Kx.Sdk.UI.Themes.FrameStyle;

namespace Kx.UI.Themes;

public sealed class FrameResource : IDisposable {
    // Original Properties
    public uFrameStyle Style { get; private set; }
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


    public bool AutoGenerateMissing { get; set; } = true;
    public SKColor PlaceholderColor { get; set; } = new SKColor(255, 0, 0, 80);
    public int DefaultCornerSize { get; set; } = 64;
    public int DefaultEdgeThickness { get; set; } = 48;
    public float DpiScale { get; private set; } = 1f;
    internal DefaultFrameResource DefaultFrame { get; private set; } = new();

    private bool _disposed;

    private FrameResource() { }

    public static FrameResource FromConfig(FrameConfig cfg, IResourceProvider provider, float dpiScale) {
        var fr = new FrameResource {
            Style = cfg.Style,
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
            DefaultFrame = DefaultFrameResource.FromConfig(cfg.Default),
        };

        fr.ApplyDpiScaling();

        return fr;
    }

    public SKRect GetContentRect(Size size) {
        if (UsesDefaultFrame)
            return GetDefaultContentRect(size);

        float width = Math.Max(0f, size.Width);
        float height = Math.Max(0f, size.Height);

        float leftWidth = LeftCenter?.Width ?? 0f;
        float rightWidth = RightCenter?.Width ?? 0f;
        float topHeight = TopCenter?.Height ?? 0f;
        float bottomHeight = BottomCenter?.Height ?? 0f;

        float fillLeft = Math.Max(0f, leftWidth - FillPosOffset);
        float fillTop = Math.Max(0f, topHeight - FillPosOffset);
        float fillRight = fillLeft + Math.Max(0f, width - leftWidth * 2 + FillWidthOffset);
        float fillBottom = fillTop + Math.Max(0f, height - topHeight - bottomHeight + FillHeightOffset);

        return new SKRect(fillLeft, fillTop, fillRight, fillBottom);
    }

    internal bool UsesDefaultFrame => Style == uFrameStyle.Default || Style == uFrameStyle.Auto && !HasCompleteImageFrame();

    internal SKRect GetTitleBarRect(Size size) {
        float width = Math.Max(0f, size.Width);
        float height = Math.Max(0f, size.Height);

        float border = Math.Max(0f, DefaultFrame.BorderThickness);
        float titleBarHeight = Math.Min(Math.Max(0f, DefaultFrame.TitleBarHeight), Math.Max(0f, height - border * 2));

        return new SKRect(border, border, Math.Max(border, width - border), border + titleBarHeight);
    }

    internal SKRect GetCloseButtonRect(Size size) {
        var titleBarRect = GetTitleBarRect(size);

        float margin = Math.Max(0f, DefaultFrame.CloseButtonMargin);
        float availableHeight = Math.Max(0f, titleBarRect.Height - margin * 2);
        float buttonSize = Math.Min(Math.Max(0f, DefaultFrame.CloseButtonSize), availableHeight);
        float top = titleBarRect.Top + Math.Max(0f, (titleBarRect.Height - buttonSize) / 2f);
        float right = titleBarRect.Right - margin;

        return new SKRect(
            Math.Max(titleBarRect.Left, right - buttonSize),
            top,
            right,
            top + buttonSize);
    }

    internal string GetTitle(string fallbackTitle) {
        return string.IsNullOrWhiteSpace(DefaultFrame.Title)
            ? fallbackTitle
            : DefaultFrame.Title;
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
        if (UsesDefaultFrame)
            return;

        if (!AutoGenerateMissing)
            return;

        int corner = (int)(DefaultCornerSize * DpiScale);
        int edge = (int)(DefaultEdgeThickness * DpiScale);

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

        DefaultFrame.ApplyDpiScaling(DpiScale);
    }

    private bool HasCompleteImageFrame() {
        return TopLeft is not null &&
               TopCenter is not null &&
               TopRight is not null &&
               RightCenter is not null &&
               BottomRight is not null &&
               BottomCenter is not null &&
               BottomLeft is not null &&
               LeftCenter is not null &&
               (UseFillColor || FillBitmap is not null);
    }

    private SKRect GetDefaultContentRect(Size size) {
        float width = Math.Max(0f, size.Width);
        float height = Math.Max(0f, size.Height);

        float border = Math.Max(0f, DefaultFrame.BorderThickness);
        float padding = Math.Max(0f, DefaultFrame.ContentPadding);
        float top = border + Math.Max(0f, DefaultFrame.TitleBarHeight) + padding;
        float left = border + padding;
        float right = Math.Max(left, width - border - padding);
        float bottom = Math.Max(top, height - border - padding);

        return new SKRect(left, top, right, bottom);
    }

    internal sealed class DefaultFrameResource {
        public string Title { get; private set; } = string.Empty;
        public SKColor BackgroundColor { get; private set; }
        public SKColor TitleBarColor { get; private set; }
        public SKColor BorderColor { get; private set; }
        public SKColor SeparatorColor { get; private set; }
        public SKColor TitleColor { get; private set; }
        public SKColor CloseButtonColor { get; private set; }
        public SKColor CloseButtonForegroundColor { get; private set; }
        public float BorderThickness { get; private set; }
        public float CornerRadius { get; private set; }
        public float TitleBarHeight { get; private set; }
        public float TitlePadding { get; private set; }
        public float TitleFontSize { get; private set; }
        public float ContentPadding { get; private set; }
        public float CloseButtonSize { get; private set; }
        public float CloseButtonMargin { get; private set; }

        public static DefaultFrameResource FromConfig(DefaultFrameConfig cfg) {
            return new DefaultFrameResource {
                Title = cfg.Title,
                BackgroundColor = SKColor.Parse(cfg.BackgroundColor),
                TitleBarColor = SKColor.Parse(cfg.TitleBarColor),
                BorderColor = SKColor.Parse(cfg.BorderColor),
                SeparatorColor = SKColor.Parse(cfg.SeparatorColor),
                TitleColor = SKColor.Parse(cfg.TitleColor),
                CloseButtonColor = SKColor.Parse(cfg.CloseButtonColor),
                CloseButtonForegroundColor = SKColor.Parse(cfg.CloseButtonForegroundColor),
                BorderThickness = Math.Max(0, cfg.BorderThickness),
                CornerRadius = Math.Max(0, cfg.CornerRadius),
                TitleBarHeight = Math.Max(0, cfg.TitleBarHeight),
                TitlePadding = Math.Max(0, cfg.TitlePadding),
                TitleFontSize = Math.Max(1, cfg.TitleFontSize),
                ContentPadding = Math.Max(0, cfg.ContentPadding),
                CloseButtonSize = Math.Max(1, cfg.CloseButtonSize),
                CloseButtonMargin = Math.Max(0, cfg.CloseButtonMargin),
            };
        }

        public void ApplyDpiScaling(float dpiScale) {
            BorderThickness *= dpiScale;
            CornerRadius *= dpiScale;
            TitleBarHeight *= dpiScale;
            TitlePadding *= dpiScale;
            TitleFontSize *= dpiScale;
            ContentPadding *= dpiScale;
            CloseButtonSize *= dpiScale;
            CloseButtonMargin *= dpiScale;
        }
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
