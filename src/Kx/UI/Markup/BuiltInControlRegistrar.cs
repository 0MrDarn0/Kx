// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Markup;
using Kx.UI.Elements;
using Kx.UI.Elements.Panel;

namespace Kx.UI.Markup;

internal static class BuiltInControlRegistrar {
    public static void Register(IControlRegistry registry) {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register("Label", (ctx, cfg) => new Kx.UI.Elements.Label(ctx, cfg.Id, cfg.Text ?? string.Empty, cfg.Font?.Size ?? 14));
        registry.Register("Button", (ctx, cfg) => new Kx.UI.Elements.Button(ctx, cfg.Id, cfg.Text ?? string.Empty));
        registry.Register("ListBox", (ctx, cfg) => new Kx.UI.Elements.ListBox(ctx, cfg.Id));
        registry.Register("TextBox", (ctx, cfg) => new Kx.UI.Elements.TextBox(ctx, cfg.Id, cfg.Text ?? string.Empty));
        registry.Register("ProgressBar", (ctx, cfg) => new Kx.UI.Elements.ProgressBar(ctx, cfg.Id));
        registry.Register("Grid", (ctx, cfg) => new Grid(ctx, cfg.Id));
        registry.Register("DockPanel", (ctx, cfg) => new DockPanel(ctx, cfg.Id));
        registry.Register("StackPanel", (ctx, cfg) => new StackPanel(ctx, cfg.Id));
    }
}
