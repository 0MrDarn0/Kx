// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Plugin;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

namespace Kx.Plugin.Theme;

public sealed class EntryPoint : IPlugin {
    private const string ContentFileSuffix = "_content.yaml";
    private const string FrameFileSuffix = "_frame.yaml";

    public string Name => "Kx.ThemePlugin";

    /// <summary>
    /// Initializes the plugin by registering window frames and content definitions from the UI folders, and applying default frame mappings based on naming conventions.
    /// </summary>
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

    private static void RegisterAllFrames(IWindowFrameRegistry frameRegistry, Kx.Sdk.Logging.ILoggingService logger) {
        ArgumentNullException.ThrowIfNull(frameRegistry);

        foreach (var framePath in Directory.EnumerateFiles(GetMarkupPath("Frames"), $"*{FrameFileSuffix}", SearchOption.TopDirectoryOnly)) {
            try {
                var frameName = GetDefinitionNameFromFilePath(framePath, FrameFileSuffix);
                frameRegistry.Register(frameName, MarkupYamlLoader.Load<WindowFrameDefinition>(framePath));
            }
            catch (Exception ex) {
                logger.Warning($"[Kx.ThemePlugin] Could not register frame from '{Path.GetFileName(framePath)}': {ex.Message}");
            }
        }
    }

    private static void RegisterAllContent(IWindowContentRegistry contentRegistry, Kx.Sdk.Logging.ILoggingService logger) {
        ArgumentNullException.ThrowIfNull(contentRegistry);

        foreach (var contentPath in Directory.EnumerateFiles(GetMarkupPath("Content"), $"*{ContentFileSuffix}", SearchOption.TopDirectoryOnly)) {
            try {
                var windowName = GetDefinitionNameFromFilePath(contentPath, ContentFileSuffix);
                var contentDefinition = MarkupYamlLoader.Load<WindowContentDefinition>(contentPath);
                contentRegistry.Register(windowName, contentDefinition);
            }
            catch (Exception ex) {
                logger.Warning($"[Kx.ThemePlugin] Could not register content from '{Path.GetFileName(contentPath)}': {ex.Message}");
            }
        }
    }

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

    private static IEnumerable<(string WindowName, string FrameName)> EnumerateDefaultFrameMappings() {
        foreach (var framePath in Directory.EnumerateFiles(GetMarkupPath("Frames"), $"*{FrameFileSuffix}", SearchOption.TopDirectoryOnly)) {
            var fileName = Path.GetFileNameWithoutExtension(framePath);
            if (fileName.EndsWith(FrameFileSuffix[..^".yaml".Length], StringComparison.OrdinalIgnoreCase)) {
                var windowName = GetDefinitionNameFromFilePath(framePath, FrameFileSuffix);
                yield return (windowName, windowName);
            }
        }
    }

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

    private static string ToPascalCase(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new InvalidOperationException($"Value '{value}' does not contain a valid definition name.");

        return string.Concat(parts.Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string GetMarkupPath(string folderName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        return Path.Combine(
            Path.GetDirectoryName(typeof(EntryPoint).Assembly.Location) ?? AppContext.BaseDirectory,
            "Markup",
            folderName);
    }
}
