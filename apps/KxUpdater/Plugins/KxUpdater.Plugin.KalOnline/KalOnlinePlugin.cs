// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

namespace KxUpdater.Plugin.KalOnline;

public sealed class KalOnlinePlugin : IPlugin {
    public string Name => "KalOnline";

    public void Initialize(IPluginContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var themeRegistry = context.Services.Get<IThemeRegistry>();
        var windowRegistry = context.Services.Get<IWindowRegistry>();

        themeRegistry.Register("KalOnline", MarkupYamlLoader.Load<WindowTheme>(GetMarkupPath("Themes", "KalOnline.yaml")));
        windowRegistry.Register("MainWindow", MarkupYamlLoader.Load<WindowConfig>(GetMarkupPath("Windows", "MainWindow.yaml")));

        context.Logger.Info($"{Name} plugin initialized");
    }

    public void Dispose() {
    }

    private static string GetMarkupPath(params string[] relativePathSegments) {
        var segments = new string[relativePathSegments.Length + 2];
        segments[0] = Path.GetDirectoryName(typeof(KalOnlinePlugin).Assembly.Location) ?? AppContext.BaseDirectory;
        segments[1] = "Markup";

        for (int i = 0; i < relativePathSegments.Length; i++)
            segments[i + 2] = relativePathSegments[i];

        return Path.Combine(segments);
    }
}
