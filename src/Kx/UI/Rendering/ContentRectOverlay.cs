// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace Kx.UI.Rendering;

internal sealed class ContentRectOverlay : IRenderOverlay {
    public const string OverlayId = "content-rect";

    public string Id => OverlayId;

    public void Draw(RenderOverlayContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var rect = context.ContentRect;

        using var paint = new SKPaint {
            Color = new SKColor(255, 0, 0, 200),
            IsStroke = true,
            StrokeWidth = 3,
            IsAntialias = true
        };

        rect.Inflate(-1, -1);

        context.Canvas.DrawRect(rect, paint);
    }
}
