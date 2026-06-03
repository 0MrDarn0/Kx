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
        registry.RegisterProperties("Label", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Text", ControlPropertyKind.String),
            new ControlPropertyDescriptor("Color", ControlPropertyKind.Color),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true)
        ]);

        registry.Register("Button", (ctx, cfg) => new Kx.UI.Elements.Button(ctx, cfg.Id, cfg.Text ?? string.Empty));
        registry.RegisterProperties("Button", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Text", ControlPropertyKind.String),
            new ControlPropertyDescriptor("OnClick", ControlPropertyKind.String),
            new ControlPropertyDescriptor("Color", ControlPropertyKind.Color),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true)
        ]);

        registry.Register("ListBox", (ctx, cfg) => new Kx.UI.Elements.ListBox(ctx, cfg.Id));
        registry.RegisterProperties("ListBox", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Color", ControlPropertyKind.Color),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true),
            new ControlPropertyDescriptor("backgroundColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("borderColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("scrollBarColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("selectedItemColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("selectedItemBorderColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("hoveredItemColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("separatorColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("borderThickness", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("glowEnabled", ControlPropertyKind.Boolean, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("glowColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("glowRadius", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag)
        ]);

        registry.Register("ServerStatus", (ctx, cfg) => new Kx.UI.Elements.ServerStatus(ctx, cfg.Id));
        registry.RegisterProperties("ServerStatus", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Color", ControlPropertyKind.Color),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true),
            new ControlPropertyDescriptor("monitoringEnabledBinding", ControlPropertyKind.String),
            new ControlPropertyDescriptor("displayNameBinding", ControlPropertyKind.String),
            new ControlPropertyDescriptor("hostBinding", ControlPropertyKind.String),
            new ControlPropertyDescriptor("showIndicator", ControlPropertyKind.Boolean, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("monitoringEnabled", ControlPropertyKind.Boolean, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("showText", ControlPropertyKind.Boolean, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("indicatorSpacing", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("displayName", ControlPropertyKind.String, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("host", ControlPropertyKind.String, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("port", ControlPropertyKind.Integer, source: ControlPropertySource.PropertiesBag)
        ]);

        registry.Register("TextBox", (ctx, cfg) => new Kx.UI.Elements.TextBox(ctx, cfg.Id, cfg.Text ?? string.Empty));
        registry.RegisterProperties("TextBox", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Text", ControlPropertyKind.String),
            new ControlPropertyDescriptor("Color", ControlPropertyKind.Color),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true),
            new ControlPropertyDescriptor("backgroundColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("borderColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("scrollBarColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("borderThickness", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("multiline", ControlPropertyKind.Boolean, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("readOnly", ControlPropertyKind.Boolean, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("glowEnabled", ControlPropertyKind.Boolean, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("glowColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("glowRadius", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag)
        ]);

        registry.Register("ProgressBar", (ctx, cfg) => new Kx.UI.Elements.ProgressBar(ctx, cfg.Id));
        registry.RegisterProperties("ProgressBar", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true),
            new ControlPropertyDescriptor("progressBinding", ControlPropertyKind.String),
            new ControlPropertyDescriptor("fillColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("backgroundColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("borderColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("borderThickness", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag)
        ]);

        registry.Register("Grid", (ctx, cfg) => new Grid(ctx, cfg.Id));
        registry.RegisterProperties("Grid", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true),
            new ControlPropertyDescriptor("Rows", ControlPropertyKind.Dictionary),
            new ControlPropertyDescriptor("Columns", ControlPropertyKind.Dictionary)
        ]);

        registry.Register("GridSplitter", (ctx, cfg) => new GridSplitter(ctx, cfg.Id));
        registry.RegisterProperties("GridSplitter", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Color", ControlPropertyKind.Color),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true),
            new ControlPropertyDescriptor("orientation", ControlPropertyKind.Enum, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("minSize", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("hoverColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("activeColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("gripColor", ControlPropertyKind.Color, source: ControlPropertySource.PropertiesBag)
        ]);

        registry.Register("DockPanel", (ctx, cfg) => new DockPanel(ctx, cfg.Id));
        registry.RegisterProperties("DockPanel", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true)
        ]);

        registry.Register("StackPanel", (ctx, cfg) => new StackPanel(ctx, cfg.Id));
        registry.RegisterProperties("StackPanel", [
            new ControlPropertyDescriptor("Type", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Id", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Layer", ControlPropertyKind.String, isCommon: true),
            new ControlPropertyDescriptor("Bounds", ControlPropertyKind.Bounds, isCommon: true),
            new ControlPropertyDescriptor("orientation", ControlPropertyKind.Enum, source: ControlPropertySource.PropertiesBag),
            new ControlPropertyDescriptor("spacing", ControlPropertyKind.Float, source: ControlPropertySource.PropertiesBag)
        ]);
    }
}
