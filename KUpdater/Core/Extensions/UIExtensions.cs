// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Extensions;

public static class UIExtensions {
    public static Rectangle ContentBounds(this WindowContext ctx) {
        var size = new Size(ctx.Target.Width, ctx.Target.Height);
        return ctx.Renderer.GetContentRect(size).ToRectangle();
    }
}
