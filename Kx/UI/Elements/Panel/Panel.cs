// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.VisualTree;

using SkiaSharp;

namespace Kx.UI.Elements.Panel;

public abstract class Panel(IVisualContext ctx, string id) : UIElement(ctx, id), IVisualContainer {
    public List<UIElement> Children { get; } = [];

    IReadOnlyList<IVisual> IVisualContainer.Children => Children;

    public void AddChild(UIElement child) {
        if (child == null)
            return;
        child.SetParent(this);
        Children.Add(child);
    }

    public bool RemoveChild(UIElement child) {
        if (child == null)
            return false;
        var removed = Children.Remove(child);
        if (removed)
            child.SetParent(null);
        return removed;
    }

    public override void Draw(SKCanvas canvas) {
        base.Draw(canvas);
        foreach (var child in Children)
            if (child.Visible)
                child.Draw(canvas);
    }

    protected override void OnDraw(SKCanvas canvas) { }

}
