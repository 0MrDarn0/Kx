// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.UI.Themes;

namespace KUpdater.UI.Markup;

public class WindowConfig {
    public FrameConfig Frame { get; set; } = new();
    public List<ControlConfig> Controls { get; set; } = new();
}
