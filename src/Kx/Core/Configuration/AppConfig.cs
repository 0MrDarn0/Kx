// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Configuration;

public class AppConfig {
    public UpdaterConfig Updater { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
}
public class UiConfig {
    public string Engine { get; set; } = "CSharp";
    public string Language { get; set; } = "en";
}
public class UpdaterConfig {
    public string Url { get; set; } = "http://webhost.com/KUpdater/";
}
