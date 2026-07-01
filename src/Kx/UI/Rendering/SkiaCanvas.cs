// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Rendering;

using SkiaSharp;

namespace Kx.UI.Rendering;

/// <summary>
/// Bridges the SDK drawing abstraction to the active SkiaSharp canvas instance.
/// </summary>
internal sealed class SkiaCanvas : IKxCanvas {
    private readonly SKCanvas _canvas;

    /// <summary>
    /// Initializes a wrapper for a concrete SkiaSharp canvas.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas used by the renderer.</param>
    public SkiaCanvas(SKCanvas canvas) {
        ArgumentNullException.ThrowIfNull(canvas);
        _canvas = canvas;
    }

    /// <summary>
    /// Tries to expose the wrapped backend object for interop scenarios.
    /// </summary>
    /// <typeparam name="TBackend">The requested backend type.</typeparam>
    /// <param name="backend">The backend object when the type matches.</param>
    /// <returns><see langword="true"/> when the backend type matches; otherwise <see langword="false"/>.</returns>
    public bool TryGetBackend<TBackend>(out TBackend? backend) where TBackend : class {
        if (_canvas is TBackend typedCanvas) {
            backend = typedCanvas;
            return true;
        }

        backend = null;
        return false;
    }

    public void DrawBitmap(object bitmap, float left, float top, float right, float bottom) {
        ArgumentNullException.ThrowIfNull(bitmap);

        if (bitmap is not SKBitmap skBitmap)
            throw new ArgumentException("Unsupported bitmap backend object.", nameof(bitmap));

        _canvas.DrawBitmap(skBitmap, new SKRect(left, top, right, bottom));
    }

    public void DrawRect(float left, float top, float right, float bottom, KxColor color) {
        using var paint = new SKPaint {
            IsAntialias = true,
            Color = color.ToSKColor()
        };

        _canvas.DrawRect(new SKRect(left, top, right, bottom), paint);
    }

    public void DrawRectStroke(float left, float top, float right, float bottom, KxColor color, float thickness = 1f) {
        using var paint = new SKPaint {
            IsAntialias = true,
            Color = color.ToSKColor(),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(0f, thickness)
        };

        _canvas.DrawRect(new SKRect(left, top, right, bottom), paint);
    }

    public void DrawLine(float x0, float y0, float x1, float y1, KxColor color, float thickness = 1f) {
        using var paint = new SKPaint {
            IsAntialias = true,
            Color = color.ToSKColor(),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(0f, thickness)
        };

        _canvas.DrawLine(x0, y0, x1, y1, paint);
    }

    public void DrawRoundedRect(float left, float top, float right, float bottom, float radiusX, float radiusY, KxColor color) {
        using var paint = new SKPaint {
            IsAntialias = true,
            Color = color.ToSKColor()
        };

        _canvas.DrawRoundRect(new SKRect(left, top, right, bottom), radiusX, radiusY, paint);
    }

    public void DrawText(string text, float x, float y, float fontSize, KxColor color, string? fontFamily = null, bool bold = false, bool italic = false, object? font = null) {
        ArgumentNullException.ThrowIfNull(text);

        using var paint = new SKPaint {
            IsAntialias = true,
            Color = color.ToSKColor()
        };

        if (font is SKFont skFont) {
            _canvas.DrawText(text, x, y, skFont, paint);
            return;
        }

        using var typeface = CreateTypeface(fontFamily, bold, italic);
        using var resolvedFont = new SKFont(typeface, fontSize);

        _canvas.DrawText(text, x, y, resolvedFont, paint);
    }

    public void MeasureText(string text, float fontSize, out float width, out float height, string? fontFamily = null, bool bold = false, bool italic = false) {
        ArgumentNullException.ThrowIfNull(text);

        using var typeface = CreateTypeface(fontFamily, bold, italic);
        using var font = new SKFont(typeface, fontSize);
        font.MeasureText(text, out var textBounds);

        width = textBounds.Width;
        height = textBounds.Height;
    }
    private static SKTypeface CreateTypeface(string? fontFamily, bool bold, bool italic) {
        string resolvedFamily = string.IsNullOrWhiteSpace(fontFamily) ? "Segoe UI" : fontFamily;
        SKFontStyleWeight weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        return SKTypeface.FromFamilyName(resolvedFamily, weight, SKFontStyleWidth.Normal, slant) ?? SKTypeface.Default;
    }
}
