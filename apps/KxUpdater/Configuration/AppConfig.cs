// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KxUpdater.Configuration;

public class AppConfig {
    public UpdaterConfig Updater { get; set; } = new();
    public LauncherConfig Launcher { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
    public ServerStatusConfig ServerStatus { get; set; } = new();
}

public class UiConfig {
    public string Language { get; set; } = "en";
}

public class UpdaterConfig {
    public string Url { get; set; } = "https://update.idb-lab.de/";
}

public class LauncherConfig {
    public ProcessLaunchConfig Start { get; set; } = new();
    public ProcessLaunchConfig Settings { get; set; } = new();
    public WebsiteLaunchConfig Website { get; set; } = new();
}

public class ProcessLaunchConfig {
    public string FileName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool ResolveFromAppDirectory { get; set; } = true;
    public bool CloseUpdaterOnSuccess { get; set; }
}

public class WebsiteLaunchConfig {
    public string Url { get; set; } = string.Empty;
}

public class ServerStatusConfig {
    public bool Enabled { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public int CheckIntervalSeconds { get; set; } = 15;
    public int ConnectTimeoutMilliseconds { get; set; } = 1500;
}
