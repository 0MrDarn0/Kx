// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.RegularExpressions;

namespace KUpdater.Scripting.Runtime;

public static class LuaKeys {
    // Hilfsfunktion: wandelt CamelCase in snake_case um
    private static string Key(string name) {

        if (name == nameof(Skin.Dir))
            return "SKIN_DIR";

        // Insert underscore before capitals, then lowercase
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2");
        return snake.ToLowerInvariant();
    }

    public static readonly string ExeDirectory = Key(nameof(ExeDirectory));

    public static class UI {
        public static readonly string AddLabel      = Key(nameof(AddLabel));
        public static readonly string AddButton     = Key(nameof(AddButton));
        public static readonly string GetWindowSize = Key(nameof(GetWindowSize));
    }

    public static class Skin {
        public static readonly string Load = Key(nameof(Load));
        public static readonly string Get  = Key(nameof(Get));
        public static readonly string Dir  = Key(nameof(Dir));
    }

    public static class Actions {
        public static readonly string StartGame       = Key(nameof(StartGame));
        public static readonly string OpenSettings    = Key(nameof(OpenSettings));
        public static readonly string ApplicationExit = Key(nameof(ApplicationExit));
        public static readonly string RunUpdate       = Key(nameof(RunUpdate));
        public static readonly string CheckUpdate     = Key(nameof(CheckUpdate));
    }

    public static class Classes {
        public static readonly string Updater = Key(nameof(Updater));
    }
}
