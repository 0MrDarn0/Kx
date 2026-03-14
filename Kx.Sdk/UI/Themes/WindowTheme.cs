// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Markup;

namespace Kx.Sdk.UI.Themes;

public class WindowTheme {
    public FrameConfig Frame { get; set; } = new();
    public List<ControlConfig> Controls { get; set; } = new();
}
