// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace Kx.UI.Rendering;

internal sealed class DebugRasterOverlay : IRenderOverlay {
    public const string OverlayId = "debug-raster";

    public string Id => OverlayId;

    public void Draw(RenderOverlayContext context) {
        ArgumentNullException.ThrowIfNull(context);

        SKCanvas canvas = context.Canvas;
        float scale = context.DeviceScale;

        int width = context.Size.Width;
        int height = context.Size.Height;

        int basNumberSpacing = 80;
        int numberSpacing = Math.Max(8, (int)(basNumberSpacing * scale));

        int baseRasterSpacing = 25;
        int rasterSpacing = Math.Max(8, (int)(baseRasterSpacing * scale));
        float mouseMarkerSize = 4f;

        using var linePaint = new SKPaint {
            Color = new SKColor(0xFF, 0xFF, 0xFF, 0x28),
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var axisPaint = new SKPaint {
            Color = new SKColor(0xFF, 0xFF, 0x00, 0xFF),
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var textPaint = new SKPaint {
            Color = new SKColor(0, 255, 100, 220),
            IsAntialias = true
        };

        float fontSize = Math.Max(7f, 12f * scale);
        using var font = new SKFont(SKTypeface.Default, fontSize);

        for (int x = 0; x < width; x += rasterSpacing)
            canvas.DrawLine(x, 0, x, height, linePaint);

        for (int y = 0; y < height; y += rasterSpacing)
            canvas.DrawLine(0, y, width, y, linePaint);

        canvas.DrawLine(0, 0, width, 0, axisPaint);
        canvas.DrawLine(0, 0, 0, height, axisPaint);

        var metrics = font.Metrics;
        float baselineOffset = -(metrics.Descent + metrics.Ascent) / 2f;

        for (int x = 0; x < width; x += numberSpacing) {
            string sx = x.ToString();
            canvas.DrawText(sx, x + 2, 2 + baselineOffset + fontSize, SKTextAlign.Left, font, textPaint);
        }

        for (int y = 0; y < height; y += numberSpacing) {
            string sy = y.ToString();
            canvas.DrawText(sy, 2, y + 2 + baselineOffset + fontSize, SKTextAlign.Left, font, textPaint);
        }

        try {
            var cursorScreen = System.Windows.Forms.Cursor.Position;
            int cursorX = cursorScreen.X - context.WindowContext.Target.Left;
            int cursorY = cursorScreen.Y - context.WindowContext.Target.Top;
            if (cursorX >= 0 && cursorX < width && cursorY >= 0 && cursorY < height) {
                using var cursorPaint = new SKPaint { Color = SKColors.Lime, IsAntialias = true };
                canvas.DrawCircle(cursorX, cursorY, mouseMarkerSize * scale, cursorPaint);
                string pos = $"{cursorX},{cursorY}";
                canvas.DrawText(pos, cursorX + 8f * scale, cursorY - 8f * scale, SKTextAlign.Left, font, textPaint);
                context.RequestRender();
            }
        }
        catch {
        }
    }
}
