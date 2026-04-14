// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.UI.Themes;

namespace KUpdater.UI.Markup;

public class ControlConfig {
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    public string? Text { get; set; }
    public string? SkinKey { get; set; }
    public string? Color { get; set; }
    public BoundsConfig? Bounds { get; set; }
    public string Layer { get; set; } = "Content";
    public string? OnClick { get; set; }
    public FontConfig? Font { get; set; }
}
