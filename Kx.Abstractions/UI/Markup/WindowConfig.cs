// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI.Themes;

namespace Kx.Abstractions.UI.Markup;

public class WindowConfig {
    public string? Theme { get; set; }
    public FrameConfig Frame { get; set; } = new();
    public List<ControlConfig> Controls { get; set; } = new();
}
