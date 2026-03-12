// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.UI.Themes;

namespace Kx.UI.Markup;

public class WindowConfig {
    public FrameConfig Frame { get; set; } = new();
    public List<ControlConfig> Controls { get; set; } = new();
}
