// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace KUpdater.Utility;

public static class InkGasm {
    public static void DrenchGlowOut(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKFont font,
        SKPaint basePaint,
        float blur = 4f,
        byte alpha = 180) {
        using var glow = basePaint.Clone();
        glow.Color = basePaint.Color.WithAlpha(alpha);
        glow.ImageFilter = SKImageFilter.CreateBlur(blur, blur);
        glow.IsAntialias = true;

        canvas.DrawText(text, x, y - 5, SKTextAlign.Center, font, glow);
        canvas.DrawText(text, x, y, SKTextAlign.Center, font, basePaint);
    }

    public static void DrenchStrokeFill(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKFont font,
        SKPaint fillPaint,
        SKColor outlineColor,
        float strokeWidth = 2f) {
        using var outline = new SKPaint
        {
            IsAntialias = true,
            Color = outlineColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth
        };

        canvas.DrawText(text, x, y, SKTextAlign.Center, font, outline);
        canvas.DrawText(text, x, y, SKTextAlign.Center, font, fillPaint);
    }

    public static void DrenchGradient(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKFont font,
        SKColor start,
        SKColor end) {
        using var gradient = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(x - 40, y - 20),
                new SKPoint(x + 40, y + 20),
                new[] { start, end },
                null,
                SKShaderTileMode.Clamp)
        };

        canvas.DrawText(text, x, y, SKTextAlign.Center, font, gradient);
    }

    public static void DrenchSweepShine(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKFont font,
        float offset) {
        using var shine = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(x - 100 + offset, y),
                new SKPoint(x + 100 + offset, y),
                new[]
                {
                    new SKColor(255,255,255,0),
                    new SKColor(255,255,255,180),
                    new SKColor(255,255,255,0)
                },
                new float[] { 0f, 0.5f, 1f },
                SKShaderTileMode.Clamp)
        };

        canvas.DrawText(text, x, y, SKTextAlign.Center, font, shine);
    }

    public static void DrenchEmbossedInset(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKFont font,
        SKPaint fillPaint) {
        // Highlight oben links
        using var highlight = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 255, 255, 120)
        };
        canvas.DrawText(text, x - 1, y - 1, SKTextAlign.Center, font, highlight);

        // Shadow unten rechts
        using var shadow = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0, 0, 0, 120)
        };
        canvas.DrawText(text, x + 1, y + 1, SKTextAlign.Center, font, shadow);

        // Main text
        canvas.DrawText(text, x, y, SKTextAlign.Center, font, fillPaint);
    }

    public static void DrenchShadowCutout(
        SKCanvas canvas,
        string text,
        SKRect layerRect,
        float x,
        float y,
        SKFont font) {
        canvas.SaveLayer(layerRect, null);

        // 1) Cutout
        using var cutout = new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.DstOut
        };
        canvas.DrawText(text, x, y, SKTextAlign.Center, font, cutout);

        // 2) Inner Shadow (sichtbar!)
        using var shadow = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0, 0, 0, 140),
            ImageFilter = SKImageFilter.CreateBlur(3, 3),
            BlendMode = SKBlendMode.SrcATop
        };
        canvas.DrawText(text, x, y + 1.5f, SKTextAlign.Center, font, shadow);

        canvas.Restore();
    }
}
