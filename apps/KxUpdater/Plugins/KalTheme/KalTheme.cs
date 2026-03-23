// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

namespace KxUpdater.Plugin;

public sealed class KalTheme : IPlugin {
    private static readonly WindowMarkupPair[] _windowMarkupPairs = [
        new("MainWindow", "UpdaterFrame", "KalOnline", "updater")
    ];

    public string Name => "KalTheme";

    public void Initialize(IPluginContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var themeRegistry = context.Services.Get<IThemeRegistry>();
        var windowRegistry = context.Services.Get<IWindowRegistry>();

        foreach (var windowMarkupPair in _windowMarkupPairs)
            RegisterWindowMarkupPair(themeRegistry, windowRegistry, windowMarkupPair);

        context.Logger.Info($"{Name} plugin initialized");
    }

    public void Dispose() {
    }

    private static void RegisterWindowMarkupPair(IThemeRegistry themeRegistry, IWindowRegistry windowRegistry, WindowMarkupPair windowMarkupPair) {
        ArgumentNullException.ThrowIfNull(themeRegistry);
        ArgumentNullException.ThrowIfNull(windowRegistry);

        themeRegistry.Register(windowMarkupPair.ThemeName, LoadUiDefinition<WindowTheme>(windowMarkupPair.StyleName, $"{windowMarkupPair.WindowKey}_frame.yaml"));

        var windowConfig = LoadUiDefinition<WindowConfig>(windowMarkupPair.StyleName, $"{windowMarkupPair.WindowKey}_content.yaml");
        windowConfig.Theme = string.IsNullOrWhiteSpace(windowConfig.Theme)
            ? windowMarkupPair.ThemeName
            : windowConfig.Theme;

        windowRegistry.Register(windowMarkupPair.WindowName, windowConfig);
    }

    private static T LoadUiDefinition<T>(string styleName, string fileName) where T : new() {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return MarkupYamlLoader.Load<T>(GetUiPath(styleName, fileName));
    }

    private static string GetUiPath(string styleName, string fileName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return Path.Combine(
            Path.GetDirectoryName(typeof(KalTheme).Assembly.Location) ?? AppContext.BaseDirectory,
            "UI",
            styleName,
            fileName);
    }

    private readonly record struct WindowMarkupPair(string WindowName, string ThemeName, string StyleName, string WindowKey);
}
