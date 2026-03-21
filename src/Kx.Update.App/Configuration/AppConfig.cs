// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Update.App.Configuration;

public class AppConfig {
    public UpdaterConfig Updater { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
}

public class UiConfig {
    public string Language { get; set; } = "en";
}

public class UpdaterConfig {
    public string Url { get; set; } = "http://webhost.com/KUpdater/";
}
