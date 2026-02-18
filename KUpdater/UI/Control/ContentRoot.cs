// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Core.Extensions;
using SkiaSharp;

namespace KUpdater.UI.Control;

public class ContentRoot : ControlBase {
    public List<IControl> Children { get; } = new();

    public ContentRoot(WindowContext ctx)
        : base(ctx, "content_root", ()
              => ctx.Renderer.GetContentRect(new Size(ctx.Target.Width, ctx.Target.Height)).ToRectangle()) { Layer = ControlLayer.Content; }

    public override void Draw(SKCanvas canvas) {
        var rect = Bounds;

        canvas.Save();
        canvas.Translate(rect.X, rect.Y);

        foreach (var child in Children)
            if (child.Visible)
                child.Draw(canvas);

        canvas.Restore();
    }

    private bool DispatchToChildren(Point p, Func<IControl, Point, bool> handler) {
        var rect = Bounds;
        var local = new Point(p.X - rect.X, p.Y - rect.Y);

        foreach (var child in Children)
            if (child.Visible && handler(child, local))
                return true;

        return false;
    }

    public override bool OnMouseDown(Point p)
        => DispatchToChildren(p, (c, lp) => c.OnMouseDown(lp));

    public override bool OnMouseUp(Point p)
        => DispatchToChildren(p, (c, lp) => c.OnMouseUp(lp));

    public override bool OnMouseMove(Point p)
        => DispatchToChildren(p, (c, lp) => c.OnMouseMove(lp));

    public override bool OnMouseWheel(int delta, Point p)
        => DispatchToChildren(p, (c, lp) => c.OnMouseWheel(delta, lp));

}
