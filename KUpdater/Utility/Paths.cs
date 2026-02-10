// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Attributes;

namespace KUpdater.Utility;

[ExposeToLua]
public static class Paths {
    // Basisverzeichnis der Anwendung (neben kUpdater.exe)
    public static readonly string Base = AppContext.BaseDirectory;

    // Hauptordner
    public static readonly string AppFolder   = Path.Combine(Base, "kUpdater");
    public static readonly string LuaFolder   = Path.Combine(AppFolder, "Lua");
    public static readonly string ResFolder   = Path.Combine(AppFolder, "Resources");

    // Unterordner von Lua
    public static readonly string LuaSkins   = Path.Combine(LuaFolder, "skins");
    public static readonly string LuaLang     = Path.Combine(LuaFolder, "languages");

    // Hilfsmethoden für Dateien
    public static string LuaScript(string fileName)
        => Path.Combine(LuaFolder, fileName);

    public static string LuaSkin(string fileName)
        => Path.Combine(LuaSkins, fileName);

    public static string LuaLanguage(string langCode)
        => Path.Combine(LuaLang, $"lang_{langCode}.lua");

    public static string LuaDefaultLanguage
        => Path.Combine(LuaLang, "lang_en.lua");

    public static string Resource(string fileName)
        => Path.Combine(ResFolder, fileName);

    public static string BaseFolder(string fileName)
        => Path.Combine(Base, fileName);
}
