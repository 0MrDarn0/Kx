// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Utility;

public static class Paths {
    static Paths() {
        Directory.CreateDirectory(LogFolder);
    }

    public static readonly string BaseDirectory = AppContext.BaseDirectory;
    public static string Combine(string path1, string path2) => Path.Combine(path1, path2);

    private static readonly string _applicationFolderName = ResolveApplicationFolderName();

    public static readonly string BaseFolder = ResolveBaseFolder();
    public static readonly string CfgFolder = Combine(BaseFolder, "Configs");
    public static readonly string LangFolder = Combine(BaseFolder, "Languages");
    public static readonly string ResourceFolder = BaseFolder;
    public static readonly string ResFolder = ResourceFolder;
    public static readonly string PluginFolder = Combine(BaseFolder, "Plugins");
    public static readonly string LogFolder = Combine(BaseFolder, "Logs");

    public static string GetConfig(string fileName) => Combine(CfgFolder, fileName);
    public static string GetResource(string fileName) => Combine(ResourceFolder, fileName);
    public static string GetLang(string langCode) => Combine(LangFolder, $"lang_{langCode}.yaml");
    public static string GetPlugin(string fileName) => Combine(PluginFolder, fileName);
    public static string GetLogFile(string fileName) => Combine(LogFolder, fileName);
    public static string GetDailyLogFile() => Combine(LogFolder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");

    private static string ResolveApplicationFolderName() {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath)) {
            var fileName = Path.GetFileNameWithoutExtension(processPath);
            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName;
        }

        return "Assets";
    }

    private static string ResolveBaseFolder() {
        var appSpecificFolder = Combine(BaseDirectory, _applicationFolderName);
        var legacyAssetsFolder = Combine(BaseDirectory, "Assets");

        if (HasAppContent(appSpecificFolder))
            return appSpecificFolder;

        if (HasAppContent(legacyAssetsFolder))
            return legacyAssetsFolder;

        return appSpecificFolder;
    }

    private static bool HasAppContent(string folderPath) {
        if (!Directory.Exists(folderPath))
            return false;

        return Directory.Exists(Combine(folderPath, "Configs"))
            || Directory.Exists(Combine(folderPath, "Languages"))
            || Directory.Exists(Combine(folderPath, "Plugins"));
    }
}
