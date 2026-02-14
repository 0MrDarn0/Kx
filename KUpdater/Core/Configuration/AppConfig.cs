// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Configuration;

public class AppConfig {
    public UiConfig Ui { get; set; } = new();
}

public class UiConfig {
    public string Engine { get; set; } = "CSharp";
}
