// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Utility;

public static class Paths {
    public static readonly string BaseDirectory = AppContext.BaseDirectory;
    public static string Combine(string path1, string path2) => Path.Combine(path1, path2);

    public static readonly string BaseFolder  = Combine(BaseDirectory, "kUpdater");
    public static readonly string CfgFolder   = Combine(BaseFolder, "Configs");
    public static readonly string LangFolder  = Combine(BaseFolder, "Languages");
    public static readonly string ResFolder   = Combine(BaseFolder, "Resources");
    public static readonly string LuaFolder   = Combine(BaseFolder, "Lua");
    public static readonly string LuaSkins    = Combine(LuaFolder, "skins");

    public static string GetConfig(string fileName) => Combine(CfgFolder, fileName);
    public static string GetResource(string fileName) => Combine(ResFolder, fileName);
    public static string GetLang(string langCode) => Combine(LangFolder, $"lang_{langCode}.yaml");
    public static string GetLuaScript(string fileName) => Combine(LuaFolder, fileName);
}
