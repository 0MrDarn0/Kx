// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

namespace KxUpdater.Plugin;

public sealed class KalTheme : IPlugin {
    private static readonly WindowMarkupPair[] _windowMarkupPairs = [
        new("MainWindow", "MainWindowFrame", "main"),
        new("MessageBox", "MessageBoxFrame", "msgbox"),
        new("Settings",   "SettingsFrame",   "settings")
    ];

    public string Name => "Theme.KalOnline";

    public void Initialize(IPluginContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var themeRegistry = context.Services.Get<IThemeRegistry>();
        var windowRegistry = context.Services.Get<IWindowRegistry>();

        foreach (var pair in _windowMarkupPairs)
            TryRegisterWindowMarkupPair(themeRegistry, windowRegistry, pair, context.Logger);

        context.Logger.Info($"{Name} plugin initialized");
    }

    public void Dispose() {
    }

    private static void TryRegisterWindowMarkupPair(
        IThemeRegistry themeRegistry,
        IWindowRegistry windowRegistry,
        WindowMarkupPair pair,
        Kx.Sdk.Logging.ILoggingService logger) {
        ArgumentNullException.ThrowIfNull(themeRegistry);
        ArgumentNullException.ThrowIfNull(windowRegistry);

        try {
            themeRegistry.Register(pair.ThemeName, LoadUiDefinition<WindowTheme>($"{pair.WindowKey}_frame.yaml"));

            var windowConfig = LoadUiDefinition<WindowConfig>($"{pair.WindowKey}_content.yaml");
            windowConfig.Theme = string.IsNullOrWhiteSpace(windowConfig.Theme)
                ? pair.ThemeName
                : windowConfig.Theme;

            windowRegistry.Register(pair.WindowName, windowConfig);
        }
        catch (Exception ex) {
            logger.Warning($"[{nameof(KalTheme)}] Could not register window '{pair.WindowName}': {ex.Message}");
        }
    }

    private static T LoadUiDefinition<T>(string fileName) where T : new() {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return MarkupYamlLoader.Load<T>(GetUiPath(fileName));
    }

    private static string GetUiPath(string fileName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return Path.Combine(
            Path.GetDirectoryName(typeof(KalTheme).Assembly.Location) ?? AppContext.BaseDirectory,
            "UI",
            fileName);
    }

    private readonly record struct WindowMarkupPair(string WindowName, string ThemeName, string WindowKey);
}
