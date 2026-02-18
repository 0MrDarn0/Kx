// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.UI.Control;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace KUpdater.UI.Layout;

public abstract class UIElement : ControlBase, IDockable {
    public Thickness Margin { get; set; } = new Thickness(0);
    public Thickness Padding { get; set; } = new Thickness(0);
    public Dock Dock { get; set; } = Dock.Fill;

    public Size DesiredSize { get; protected set; }
    public Rectangle LayoutRect { get; protected set; }

    public int GridRow { get; set; } = 0;
    public int GridColumn { get; set; } = 0;
    public int GridRowSpan { get; set; } = 1;
    public int GridColumnSpan { get; set; } = 1;

    protected UIElement(WindowContext ctx, string id, Func<Rectangle> boundsFunc)
        : base(ctx, id, boundsFunc) { }

    public virtual void Measure(float dpi) {
        var size = Bounds.Size;

        size.Width += (int)(Margin.Horizontal * dpi);
        size.Height += (int)(Margin.Vertical * dpi);

        DesiredSize = size;
    }

    public virtual void Arrange(Rectangle finalRect, float dpi) {
        finalRect = new Rectangle(
            finalRect.X + (int)(Margin.Left * dpi),
            finalRect.Y + (int)(Margin.Top * dpi),
            finalRect.Width - (int)(Margin.Horizontal * dpi),
            finalRect.Height - (int)(Margin.Vertical * dpi)
        );

        LayoutRect = finalRect;
    }

    public override void Draw(SKCanvas canvas) {
        base.Draw(canvas);

        OnDraw(canvas);

        if (DebugOverlay.Enabled)
            DrawDebugOverlay(canvas);
    }
    protected abstract void OnDraw(SKCanvas canvas);

    protected virtual void DrawDebugOverlay(SKCanvas canvas) {
        using var paint = new SKPaint {
            Color = new SKColor(255, 255, 255, 255),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };

        canvas.DrawRect(LayoutRect.ToSKRect(), paint);
    }

}
