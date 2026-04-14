// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Themes;

namespace Kx.Sdk.UI.Markup;

public class WindowContentDefinition {
    public string? FrameDefinition { get; set; }
    public FrameConfig Frame { get; set; } = new();
    public List<ControlConfig> Controls { get; set; } = new();
}
