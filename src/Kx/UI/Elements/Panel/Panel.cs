// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.VisualTree;
using Kx.Sdk.Rendering;

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

    public override void Draw(IKxCanvas canvas) {
        base.Draw(canvas);
        foreach (var child in Children)
            if (child.Visible)
                child.Draw(canvas);
    }

    protected override void OnDraw(IKxCanvas canvas) { }

}
