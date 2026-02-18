// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using SkiaSharp;

namespace KUpdater.UI.Layout;

public abstract class Panel : UIElement {
    public List<UIElement> Children { get; } = new();

    protected Panel(WindowContext ctx, string id, Func<Rectangle> boundsFunc)
        : base(ctx, id, boundsFunc) { }

    public override void Draw(SKCanvas canvas) {
        base.Draw(canvas);

        // Panel zeichnet seine Kinder
        foreach (var child in Children)
            if (child.Visible)
                child.Draw(canvas);
    }

    protected override void OnDraw(SKCanvas canvas) {
        // Panels haben normalerweise kein eigenes Rendering
    }
}
