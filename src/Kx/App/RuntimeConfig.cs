// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.App;

public class RuntimeConfig {
    public RuntimeUiConfig Ui { get; set; } = new();
    public RuntimeWindowConfig Window { get; set; } = new();
}

public class RuntimeUiConfig {
    public string Language { get; set; } = "en";
}

public class RuntimeWindowConfig {
    public string? Icon { get; set; }
}
