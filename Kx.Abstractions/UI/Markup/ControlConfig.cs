// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI.Markup;

public class ControlConfig {
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? SkinKey { get; set; }
    public string? Color { get; set; }
    public BoundsConfig? Bounds { get; set; }
    public ThicknessConfig? Margin { get; set; }
    public ThicknessConfig? Padding { get; set; }
    public string? Dock { get; set; }
    public string Layer { get; set; } = "Content";
    public string? OnClick { get; set; }
    public FontConfig? Font { get; set; }
    public int GridRow { get; set; }
    public int GridColumn { get; set; }
    public int GridRowSpan { get; set; } = 1;
    public int GridColumnSpan { get; set; } = 1;
    public List<GridRowConfig> Rows { get; set; } = [];
    public List<GridColumnConfig> Columns { get; set; } = [];
    public List<ControlConfig> Children { get; set; } = [];
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
