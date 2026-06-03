// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text;

using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KxEdit;

public enum EditorDocumentKind {
    Content,
    Frame
}

public sealed class MarkupEditorDocument {
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public EditorDocumentKind Kind { get; set; }
    public string? FrameDefinition { get; set; }
    public FrameConfig Frame { get; } = new();
    public List<MarkupEditorControl> Controls { get; } = [];

    public DefaultFrameConfig FrameDefault => Frame.Default;

    public static MarkupEditorDocument CreateDefault(EditorDocumentKind kind) {
        var document = new MarkupEditorDocument();
        document.Reset(kind);
        return document;
    }

    public void Reset(EditorDocumentKind kind) {
        Kind = kind;
        FrameDefinition = null;

        Frame.Style = Kx.Sdk.UI.Themes.FrameStyle.Default;
        Frame.Default.Title = kind == EditorDocumentKind.Content ? "Neue Content-Ansicht" : "Neue Frame-Definition";
        Frame.Default.BackgroundColor = "#1E1F22";
        Frame.Default.TitleBarColor = "#25262B";
        Frame.Default.BorderColor = "#3A3D46";
        Frame.Default.SeparatorColor = "#3A3D46";
        Frame.Default.TitleColor = "#F5F5F5";
        Controls.Clear();
        Controls.Add(MarkupEditorControl.CreateDefault());
    }

    public void CopyFrom(MarkupEditorDocument source) {
        ArgumentNullException.ThrowIfNull(source);

        Kind = source.Kind;
        FrameDefinition = source.FrameDefinition;

        CopyFrame(source.Frame, Frame);

        Controls.Clear();
        foreach (var control in source.Controls)
            Controls.Add(control.Clone());
    }

    public bool RemoveControl(MarkupEditorControl target) {
        ArgumentNullException.ThrowIfNull(target);

        if (Controls.Remove(target))
            return true;

        foreach (var control in Controls) {
            if (RemoveControlRecursive(control, target))
                return true;
        }

        return false;
    }

    public string ToYaml() {
        return Kind == EditorDocumentKind.Content
            ? _serializer.Serialize(ToContentDefinition())
            : _serializer.Serialize(ToFrameDefinition());
    }

    public string ToFrameYaml() {
        return _serializer.Serialize(ToFrameDefinition());
    }

    public string ToContentYaml() {
        return _serializer.Serialize(ToSplitContentDefinition());
    }

    public void Save(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, ToYaml(), Encoding.UTF8);
    }

    public MarkupEditorSaveResult SavePair(string selectedPath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPath);

        var paths = MarkupEditorSavePaths.FromSelectedPath(selectedPath);
        Directory.CreateDirectory(paths.DirectoryPath);

        File.WriteAllText(paths.FramePath, ToFrameYaml(), Encoding.UTF8);
        File.WriteAllText(paths.ContentPath, ToContentYaml(), Encoding.UTF8);

        return new MarkupEditorSaveResult(paths.FramePath, paths.ContentPath);
    }

    public static MarkupEditorDocument Load(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var pair = TryLoadPair(path);
        if (pair is not null)
            return pair;

        var yaml = File.ReadAllText(path);
        var kind = DetectKind(path, yaml);
        var document = CreateDefault(kind);

        if (kind == EditorDocumentKind.Content) {
            var content = _deserializer.Deserialize<WindowContentDefinition>(yaml) ?? new WindowContentDefinition();
            document.Kind = EditorDocumentKind.Content;
            document.FrameDefinition = content.FrameDefinition;
            CopyFrame(content.Frame, document.Frame);
            document.Controls.Clear();
            foreach (var control in content.Controls)
                document.Controls.Add(MarkupEditorControl.FromConfig(control));
        }
        else {
            var frame = _deserializer.Deserialize<WindowFrameDefinition>(yaml) ?? new WindowFrameDefinition();
            document.Kind = EditorDocumentKind.Frame;
            document.FrameDefinition = null;
            CopyFrame(frame.Frame, document.Frame);
            document.Controls.Clear();
            foreach (var control in frame.Controls)
                document.Controls.Add(MarkupEditorControl.FromConfig(control));
        }

        if (document.Controls.Count == 0)
            document.Controls.Add(MarkupEditorControl.CreateDefault());

        return document;
    }

    private static MarkupEditorDocument? TryLoadPair(string path) {
        var paths = MarkupEditorSavePaths.FromSelectedPath(path);
        if (!File.Exists(paths.FramePath) || !File.Exists(paths.ContentPath))
            return null;

        var frameYaml = File.ReadAllText(paths.FramePath);
        var contentYaml = File.ReadAllText(paths.ContentPath);
        var frame = _deserializer.Deserialize<WindowFrameDefinition>(frameYaml) ?? new WindowFrameDefinition();
        var content = _deserializer.Deserialize<WindowContentDefinition>(contentYaml) ?? new WindowContentDefinition();

        var document = CreateDefault(EditorDocumentKind.Content);
        document.FrameDefinition = content.FrameDefinition;
        CopyFrame(frame.Frame, document.Frame);
        document.Controls.Clear();

        foreach (var control in frame.Controls)
            document.Controls.Add(MarkupEditorControl.FromConfig(control));

        foreach (var control in content.Controls)
            document.Controls.Add(MarkupEditorControl.FromConfig(control));

        if (document.Controls.Count == 0)
            document.Controls.Add(MarkupEditorControl.CreateDefault());

        return document;
    }

    private WindowContentDefinition ToContentDefinition() {
        return new WindowContentDefinition {
            FrameDefinition = string.IsNullOrWhiteSpace(FrameDefinition) ? null : FrameDefinition,
            Frame = CloneFrame(Frame),
            Controls = Controls.Select(control => control.ToConfig()).ToList()
        };
    }

    private SplitWindowContentDefinition ToSplitContentDefinition() {
        return new SplitWindowContentDefinition {
            FrameDefinition = string.IsNullOrWhiteSpace(FrameDefinition) ? null : FrameDefinition,
            Controls = Controls
                .Where(control => IsContentLayer(control.Layer))
                .Select(control => control.ToConfig())
                .ToList()
        };
    }

    private WindowFrameDefinition ToFrameDefinition() {
        return new WindowFrameDefinition {
            Frame = CloneFrame(Frame),
            Controls = Controls
                .Where(control => !IsContentLayer(control.Layer))
                .Select(control => control.ToConfig())
                .ToList()
        };
    }

    private static bool IsContentLayer(string? layer) {
        return string.IsNullOrWhiteSpace(layer) || string.Equals(layer, "Content", StringComparison.OrdinalIgnoreCase);
    }

    private static EditorDocumentKind DetectKind(string path, string yaml) {
        var fileName = Path.GetFileNameWithoutExtension(path);
        if (fileName.Contains("content", StringComparison.OrdinalIgnoreCase))
            return EditorDocumentKind.Content;
        if (fileName.Contains("frame", StringComparison.OrdinalIgnoreCase))
            return EditorDocumentKind.Frame;

        return yaml.Contains("frameDefinition:", StringComparison.OrdinalIgnoreCase)
            ? EditorDocumentKind.Content
            : EditorDocumentKind.Frame;
    }

    private static bool RemoveControlRecursive(MarkupEditorControl parent, MarkupEditorControl target) {
        if (parent.Children.Remove(target))
            return true;

        foreach (var child in parent.Children) {
            if (RemoveControlRecursive(child, target))
                return true;
        }

        return false;
    }

    private static FrameConfig CloneFrame(FrameConfig source) {
        var target = new FrameConfig();
        CopyFrame(source, target);
        return target;
    }

    private static void CopyFrame(FrameConfig source, FrameConfig target) {
        target.Style = source.Style;
        target.TopLeft = source.TopLeft;
        target.TopCenter = source.TopCenter;
        target.TopRight = source.TopRight;
        target.RightCenter = source.RightCenter;
        target.BottomRight = source.BottomRight;
        target.BottomCenter = source.BottomCenter;
        target.BottomLeft = source.BottomLeft;
        target.LeftCenter = source.LeftCenter;
        target.FillBitmap = source.FillBitmap;
        target.FillColor = source.FillColor;
        target.UseFillColor = source.UseFillColor;
        target.TopWidthOffset = source.TopWidthOffset;
        target.BottomWidthOffset = source.BottomWidthOffset;
        target.LeftHeightOffset = source.LeftHeightOffset;
        target.RightHeightOffset = source.RightHeightOffset;
        target.FillPosOffset = source.FillPosOffset;
        target.FillWidthOffset = source.FillWidthOffset;
        target.FillHeightOffset = source.FillHeightOffset;

        target.Default.Title = source.Default.Title;
        target.Default.Icon = source.Default.Icon;
        target.Default.BackgroundColor = source.Default.BackgroundColor;
        target.Default.TitleBarColor = source.Default.TitleBarColor;
        target.Default.BorderColor = source.Default.BorderColor;
        target.Default.SeparatorColor = source.Default.SeparatorColor;
        target.Default.TitleColor = source.Default.TitleColor;
        target.Default.CloseButtonColor = source.Default.CloseButtonColor;
        target.Default.CloseButtonForegroundColor = source.Default.CloseButtonForegroundColor;
        target.Default.BorderThickness = source.Default.BorderThickness;
        target.Default.CornerRadius = source.Default.CornerRadius;
        target.Default.TitleBarHeight = source.Default.TitleBarHeight;
        target.Default.TitlePadding = source.Default.TitlePadding;
        target.Default.TitleFontSize = source.Default.TitleFontSize;
        target.Default.ContentPadding = source.Default.ContentPadding;
        target.Default.CloseButtonSize = source.Default.CloseButtonSize;
        target.Default.CloseButtonMargin = source.Default.CloseButtonMargin;
    }
}

public sealed record MarkupEditorSaveResult(string FramePath, string ContentPath);

public sealed record MarkupEditorSavePaths(string DirectoryPath, string BaseName, string FramePath, string ContentPath) {
    public static MarkupEditorSavePaths FromSelectedPath(string selectedPath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPath);

        var directory = Path.GetDirectoryName(selectedPath);
        if (string.IsNullOrWhiteSpace(directory))
            directory = Directory.GetCurrentDirectory();

        var baseName = NormalizeBaseName(Path.GetFileNameWithoutExtension(selectedPath));
        var framePath = Path.Combine(directory, $"{baseName}_frame.yaml");
        var contentPath = Path.Combine(directory, $"{baseName}_content.yaml");

        return new MarkupEditorSavePaths(directory, baseName, framePath, contentPath);
    }

    private static string NormalizeBaseName(string fileNameWithoutExtension) {
        var name = string.IsNullOrWhiteSpace(fileNameWithoutExtension) ? "window" : fileNameWithoutExtension.Trim();

        if (name.EndsWith("_content", StringComparison.OrdinalIgnoreCase))
            return name[..^"_content".Length];
        if (name.EndsWith("_frame", StringComparison.OrdinalIgnoreCase))
            return name[..^"_frame".Length];

        return name;
    }
}

internal sealed class SplitWindowContentDefinition {
    public string? FrameDefinition { get; set; }
    public List<ControlConfig> Controls { get; set; } = [];
}

public sealed class MarkupEditorControl {
    public string Type { get; set; } = "Label";
    public string Id { get; set; } = "new_control";
    public string? Text { get; set; } = "Neues Control";
    public string Layer { get; set; } = "Content";
    public string? Color { get; set; }
    public string? OnClick { get; set; }
    public BoundsConfig Bounds { get; set; } = new() { X = 24, Y = 24, Width = 220, Height = 56 };
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<MarkupEditorControl> Children { get; } = [];

    public static MarkupEditorControl CreateDefault() {
        return CreateDefault("Label");
    }

    public static MarkupEditorControl CreateDefault(string type) {
        string normalizedType = string.IsNullOrWhiteSpace(type) ? "Label" : type.Trim();
        string defaultText = normalizedType switch {
            "Button" => "Button",
            "TextBox" => "Text eingeben",
            "ListBox" => "Liste",
            "StackPanel" => "StackPanel",
            "Grid" => "Grid",
            _ => "Neues Label"
        };

        int defaultWidth = normalizedType switch {
            "Button" => 180,
            "TextBox" => 240,
            "ListBox" => 260,
            "StackPanel" => 280,
            "Grid" => 320,
            _ => 260
        };

        int defaultHeight = normalizedType switch {
            "Button" => 42,
            "TextBox" => 42,
            "ListBox" => 140,
            "StackPanel" => 180,
            "Grid" => 180,
            _ => 42
        };

        return new MarkupEditorControl {
            Type = normalizedType,
            Id = $"control_{Guid.NewGuid():N}"[..12],
            Text = defaultText,
            Layer = "Content",
            Bounds = new BoundsConfig {
                X = 24,
                Y = 24,
                Width = defaultWidth,
                Height = defaultHeight
            }
        };
    }

    public MarkupEditorControl Clone() {
        var copy = new MarkupEditorControl {
            Type = Type,
            Id = Id,
            Text = Text,
            Layer = Layer,
            Color = Color,
            OnClick = OnClick,
            Bounds = new BoundsConfig {
                X = Bounds.X,
                Y = Bounds.Y,
                Width = Bounds.Width,
                Height = Bounds.Height
            },
            Properties = new Dictionary<string, string>(Properties, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var child in Children)
            copy.Children.Add(child.Clone());

        return copy;
    }

    public static MarkupEditorControl FromConfig(ControlConfig config) {
        var control = new MarkupEditorControl {
            Type = string.IsNullOrWhiteSpace(config.Type) ? "Label" : config.Type,
            Id = string.IsNullOrWhiteSpace(config.Id) ? $"control_{Guid.NewGuid():N}"[..12] : config.Id,
            Text = config.Text,
            Layer = string.IsNullOrWhiteSpace(config.Layer) ? "Content" : config.Layer,
            Color = config.Color,
            OnClick = config.OnClick,
            Bounds = config.Bounds is null
                ? new BoundsConfig { X = 24, Y = 24, Width = 220, Height = 56 }
                : new BoundsConfig {
                    X = config.Bounds.X,
                    Y = config.Bounds.Y,
                    Width = config.Bounds.Width,
                    Height = config.Bounds.Height
                },
            Properties = new Dictionary<string, string>(config.Properties, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var child in config.Children)
            control.Children.Add(FromConfig(child));

        return control;
    }

    public ControlConfig ToConfig() {
        var config = new ControlConfig {
            Type = Type,
            Id = Id,
            Layer = Layer,
            Bounds = new BoundsConfig {
                X = Bounds.X,
                Y = Bounds.Y,
                Width = Bounds.Width,
                Height = Bounds.Height
            },
            Properties = new Dictionary<string, string>(Properties, StringComparer.OrdinalIgnoreCase)
        };

        if (!string.IsNullOrWhiteSpace(Text))
            config.Text = Text;
        if (!string.IsNullOrWhiteSpace(Color))
            config.Color = Color;
        if (!string.IsNullOrWhiteSpace(OnClick))
            config.OnClick = OnClick;

        foreach (var child in Children)
            config.Children.Add(child.ToConfig());

        return config;
    }
}
