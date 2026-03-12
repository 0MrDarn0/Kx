// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core;
using SkiaSharp;

namespace Kx.UI.Elements.Panel;

public abstract class Panel(WindowContext ctx, string id) : UIElement(ctx, id) {
    public List<UIElement> Children { get; } = [];

    public void AddChild(UIElement child) {
        if (child == null)
            return;
        child.Parent = this;
        Children.Add(child);
    }

    public bool RemoveChild(UIElement child) {
        if (child == null)
            return false;
        var removed = Children.Remove(child);
        if (removed)
            child.Parent = null;
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
