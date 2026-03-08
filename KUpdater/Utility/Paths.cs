// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Utility;

public static class Paths {
    static Paths() {
        Directory.CreateDirectory(LogFolder);
    }

    public static readonly string BaseDirectory = AppContext.BaseDirectory;
    public static string Combine(string path1, string path2) => Path.Combine(path1, path2);

    public static readonly string BaseFolder  = Combine(BaseDirectory, "kUpdater");
    public static readonly string CfgFolder   = Combine(BaseFolder, "Configs");
    public static readonly string LangFolder  = Combine(BaseFolder, "Languages");
    public static readonly string ResFolder   = Combine(BaseFolder, "Resources");
    public static readonly string PluginFolder  = Combine(BaseFolder, "Plugins");
    public static readonly string LogFolder   = Combine(BaseFolder, "Logs");

    public static string GetConfig(string fileName) => Combine(CfgFolder, fileName);
    public static string GetResource(string fileName) => Combine(ResFolder, fileName);
    public static string GetLang(string langCode) => Combine(LangFolder, $"lang_{langCode}.yaml");
    public static string GetPlugin(string fileName) => Combine(PluginFolder, fileName);
    public static string GetLogFile(string fileName) => Combine(LogFolder, fileName);
    public static string GetDailyLogFile() => Combine(LogFolder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
}
