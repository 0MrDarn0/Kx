// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

namespace KxUpdater.Plugin;

public sealed class KalTheme : IPlugin {
    private static readonly WindowDefinitionPair[] _windowDefinitionPairs = [
        new("MainWindow", "MainWindowFrame", "main"),
        new("MessageBox", "MessageBoxFrame", "msgbox"),
        new("Settings",   "SettingsFrame",   "settings")
    ];

    public string Name => "Theme.KalOnline";

    public void Initialize(IPluginContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var frameRegistry = context.Services.Get<IWindowFrameRegistry>();
        var contentRegistry = context.Services.Get<IWindowContentRegistry>();

        foreach (var pair in _windowDefinitionPairs)
            TryRegisterWindowDefinition(frameRegistry, contentRegistry, pair, context.Logger);

        context.Logger.Info($"{Name} plugin initialized");
    }

    public void Dispose() {
    }

    private static void TryRegisterWindowDefinition(
        IWindowFrameRegistry frameRegistry,
        IWindowContentRegistry contentRegistry,
        WindowDefinitionPair pair,
        Kx.Sdk.Logging.ILoggingService logger) {
        ArgumentNullException.ThrowIfNull(frameRegistry);
        ArgumentNullException.ThrowIfNull(contentRegistry);

        try {
            frameRegistry.Register(pair.FrameDefinitionName, LoadFrameDefinition($"{pair.ResourceKey}_frame.yaml"));

            var contentDefinition = LoadContentDefinition($"{pair.ResourceKey}_content.yaml");
            contentDefinition.FrameDefinition = string.IsNullOrWhiteSpace(contentDefinition.FrameDefinition)
                ? pair.FrameDefinitionName
                : contentDefinition.FrameDefinition;

            contentRegistry.Register(pair.WindowName, contentDefinition);
        }
        catch (Exception ex) {
            logger.Warning($"[{nameof(KalTheme)}] Could not register window '{pair.WindowName}': {ex.Message}");
        }
    }

    private static WindowFrameDefinition LoadFrameDefinition(string fileName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return MarkupYamlLoader.Load<WindowFrameDefinition>(GetUiPath("Frames", fileName));
    }

    private static WindowContentDefinition LoadContentDefinition(string fileName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return MarkupYamlLoader.Load<WindowContentDefinition>(GetUiPath("Content", fileName));
    }

    private static string GetUiPath(string folderName, string fileName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return Path.Combine(
            Path.GetDirectoryName(typeof(KalTheme).Assembly.Location) ?? AppContext.BaseDirectory,
            "UI",
            folderName,
            fileName);
    }

    private readonly record struct WindowDefinitionPair(string WindowName, string FrameDefinitionName, string ResourceKey);
}
