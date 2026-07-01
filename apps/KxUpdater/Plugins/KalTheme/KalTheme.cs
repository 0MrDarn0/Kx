// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

namespace KxUpdater.Plugin;

public sealed class KalTheme : IPlugin {
    private const string ContentFileSuffix = "_content.yaml";
    private const string FrameFileSuffix = "_frame.yaml";

    public string Name => "Theme.KalOnline";


    // Initializes the plugin by registering window frames and content definitions from the UI folders, and applying default frame mappings based on naming conventions.
    public void Initialize(IPluginContext context) {
        ArgumentNullException.ThrowIfNull(context);

        var frameRegistry = context.Services.Get<IWindowFrameRegistry>();
        var contentRegistry = context.Services.Get<IWindowContentRegistry>();

        RegisterAllFrames(frameRegistry, context.Logger);
        RegisterAllContent(contentRegistry, context.Logger);
        ApplyDefaultFrameMappings(contentRegistry);

        context.Logger.Info($"{Name} plugin initialized");
    }


    public void Dispose() { }


    /// Registers all window frame definitions found in the "Frames" UI folder, using the file naming convention to determine the frame name.
    private static void RegisterAllFrames(IWindowFrameRegistry frameRegistry, Kx.Sdk.Logging.ILoggingService logger) {
        ArgumentNullException.ThrowIfNull(frameRegistry);

        foreach (var framePath in Directory.EnumerateFiles(GetUiPath("Frames"), $"*{FrameFileSuffix}", SearchOption.TopDirectoryOnly)) {
            try {
                var frameName = GetDefinitionNameFromFilePath(framePath, FrameFileSuffix);
                frameRegistry.Register(frameName, MarkupYamlLoader.Load<WindowFrameDefinition>(framePath));
            }
            catch (Exception ex) {
                logger.Warning($"[{nameof(KalTheme)}] Could not register frame from '{Path.GetFileName(framePath)}': {ex.Message}");
            }
        }
    }

    /// Registers all window content definitions found in the "Content" UI folder, using the file naming convention to determine the window name.
    private static void RegisterAllContent(IWindowContentRegistry contentRegistry, Kx.Sdk.Logging.ILoggingService logger) {
        ArgumentNullException.ThrowIfNull(contentRegistry);

        foreach (var contentPath in Directory.EnumerateFiles(GetUiPath("Content"), $"*{ContentFileSuffix}", SearchOption.TopDirectoryOnly)) {
            try {
                var windowName = GetDefinitionNameFromFilePath(contentPath, ContentFileSuffix);
                var contentDefinition = MarkupYamlLoader.Load<WindowContentDefinition>(contentPath);
                contentRegistry.Register(windowName, contentDefinition);
            }
            catch (Exception ex) {
                logger.Warning($"[{nameof(KalTheme)}] Could not register content from '{Path.GetFileName(contentPath)}': {ex.Message}");
            }
        }
    }

    /// Applies default frame mappings to content definitions that do not have an explicitly defined frame, based on the naming convention of frame definition files.
    private static void ApplyDefaultFrameMappings(IWindowContentRegistry contentRegistry) {
        ArgumentNullException.ThrowIfNull(contentRegistry);

        foreach (var (windowName, frameName) in EnumerateDefaultFrameMappings()) {
            if (!contentRegistry.TryGet(windowName, out var contentDefinition) || contentDefinition is null)
                continue;

            if (!string.IsNullOrWhiteSpace(contentDefinition.FrameDefinition))
                continue;

            contentDefinition.FrameDefinition = frameName;
        }
    }

    /// Enumerates pairs of window names and frame names based on the frame definition files in the "Frames" UI folder.
    private static IEnumerable<(string WindowName, string FrameName)> EnumerateDefaultFrameMappings() {
        foreach (var framePath in Directory.EnumerateFiles(GetUiPath("Frames"), $"*{FrameFileSuffix}", SearchOption.TopDirectoryOnly)) {
            var fileName = Path.GetFileNameWithoutExtension(framePath);
            if (fileName.EndsWith(FrameFileSuffix[..^".yaml".Length], StringComparison.OrdinalIgnoreCase)) {
                var windowName = GetDefinitionNameFromFilePath(framePath, FrameFileSuffix);
                yield return (windowName, windowName);
            }
        }
    }

    /// Extracts a definition name from a file path by removing the specified suffix and converting the remaining name to PascalCase.
    private static string GetDefinitionNameFromFilePath(string filePath, string suffix) {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(suffix);

        var fileName = Path.GetFileName(filePath);
        if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"File '{fileName}' must end with '{suffix}'.");

        var rawName = fileName[..^suffix.Length];
        if (rawName.Length == 0)
            throw new InvalidOperationException($"File '{fileName}' must contain a name before '{suffix}'.");

        return ToPascalCase(rawName);
    }

    /// Converts a string to PascalCase by splitting on common delimiters and capitalizing each part.
    private static string ToPascalCase(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new InvalidOperationException($"Value '{value}' does not contain a valid definition name.");

        return string.Concat(parts.Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    /// Constructs the full path to a UI definition folder based on the assembly location and the specified folder name.
    private static string GetUiPath(string folderName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        return Path.Combine(
            Path.GetDirectoryName(typeof(KalTheme).Assembly.Location) ?? AppContext.BaseDirectory,
            "UI",
            folderName);
    }
}
