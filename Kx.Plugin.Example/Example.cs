// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Sdk.Plugin;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Payloads;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.Themes;

using SkiaSharp;

namespace Kx.Plugin;

public sealed class Example : IPlugin {
    private static readonly UiStateKey<string> TitleState = new("example.title");
    private static readonly UiStateKey<string> TitleColorState = new("example.titleColor");
    private static readonly UiStateKey<float> TitleFontSizeState = new("example.titleFontSize");
    private static readonly UiStateKey<string> BadgeTextState = new("example.badgeText");
    private static readonly UiStateKey<bool> MergeHintVisibleState = new("example.mergeHintVisible");
    private static readonly UiStateKey<float> PanelSpacingState = new("example.panelSpacing");
    private static readonly UiStateKey<string> PanelOrientationState = new("example.panelOrientation");
    private static readonly UiStateKey<float> ButtonFontSizeState = new("example.buttonFontSize");

    public string Name => "Example";

    public void Initialize(IPluginContext context) {
        var controlRegistry = context.Services.Get<IControlRegistry>();
        var actionRegistry = context.Services.Get<IMarkupActionRegistry>();
        var commandRegistry = context.Services.Get<IUiCommandRegistry>();
        var stateStore = context.Services.Get<IUiStateStore>();
        var themeRegistry = context.Services.Get<IThemeRegistry>();
        var windowRegistry = context.Services.Get<IWindowRegistry>();

        stateStore.Set(TitleState, "  Plugin Window (bound)  ");
        stateStore.Set(TitleColorState, "#F5F5F5");
        stateStore.Set(TitleFontSizeState, 16f);
        stateStore.Set(BadgeTextState, "Plugin Content");
        stateStore.Set(MergeHintVisibleState, true);
        stateStore.Set(PanelSpacingState, 8f);
        stateStore.Set(PanelOrientationState, "Vertical");
        stateStore.Set(ButtonFontSizeState, 14f);

        controlRegistry.Register("ExampleBadge", (uiContext, config) => new ExampleBadge(uiContext, config.Id, config.Text, config.Properties.TryGetValue("textState", out var textState) ? textState : null));
        actionRegistry.Register("example.toggleVisibility", actionContext => ToggleVisibility(actionContext));
        commandRegistry.Register("example.renameBadge", commandContext => RenameBadge(stateStore, commandContext));
        RegisterTheme(themeRegistry, "Example.Dark", "Themes", "Example.Dark.yaml");
        RegisterTheme(themeRegistry, "Example.Alternate", "Themes", "Example.Alternate.yaml");

        RegisterWindow(windowRegistry, "MainWindow", "Windows", "MainWindow.yaml");
        RegisterWindow(windowRegistry, "Example.Alternate", "Windows", "Example.Alternate.yaml");

        context.Logger.Info($"{Name} initialized");
        context.Logger.Info($"ApiVersion: {context.ApiVersion}");
    }

    public void Dispose() {

    }

    private static void ToggleVisibility(IMarkupActionContext actionContext) {
        if (!UiTargetResolver.TryResolve(actionContext.Source, actionContext.Argument, out var visual) || visual is null)
            return;

        visual.Visible = !visual.Visible;
    }

    private static void RenameBadge(IUiStateStore stateStore, IUiCommandContext commandContext) {
        if (!commandContext.Payload.TryDeserialize<TextUpdatePayload>(out var payload) || payload is null)
            return;

        stateStore.Set(BadgeTextState, payload.Text);
    }

    private static void RegisterTheme(IThemeRegistry themeRegistry, string name, params string[] relativePathSegments) {
        ArgumentNullException.ThrowIfNull(themeRegistry);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        themeRegistry.Register(name, MarkupYamlLoader.Load<WindowTheme>(GetMarkupPath(relativePathSegments)));
    }

    private static void RegisterWindow(IWindowRegistry windowRegistry, string name, params string[] relativePathSegments) {
        ArgumentNullException.ThrowIfNull(windowRegistry);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        windowRegistry.Register(name, MarkupYamlLoader.Load<WindowConfig>(GetMarkupPath(relativePathSegments)));
    }

    private static string GetMarkupPath(params string[] relativePathSegments) {
        var segments = new string[relativePathSegments.Length + 2];
        segments[0] = Path.GetDirectoryName(typeof(Example).Assembly.Location) ?? AppContext.BaseDirectory;
        segments[1] = "Markup";

        for (int i = 0; i < relativePathSegments.Length; i++)
            segments[i + 2] = relativePathSegments[i];

        return Path.Combine(segments);
    }

    private sealed class ExampleBadge : UIElement {
        private string _text;

        public ExampleBadge(IVisualContext context, string id, string? text, string? textStatePath) : base(context, id) {
            _text = string.IsNullOrWhiteSpace(text) ? "Example" : text;

            if (string.IsNullOrWhiteSpace(textStatePath))
                return;

            var stateKey = new UiStateKey<string>(textStatePath);

            if (Context.State.TryGet(stateKey, out var currentText) && currentText is not null)
                _text = currentText;

            TrackDisposable(Context.State.Subscribe(stateKey, boundText => {
                if (boundText is not null)
                    SetText(boundText);
            }));
        }

        public void SetText(string text) {
            _text = string.IsNullOrWhiteSpace(text) ? "Example" : text;
            Context.RequestRender();
        }

        public override void Measure(float dpi) {
            DesiredSize = new Size((int)(160 * dpi), (int)(40 * dpi));
        }

        protected override void OnDraw(SKCanvas canvas) {
            using var backgroundPaint = new SKPaint {
                IsAntialias = true,
                Color = new SKColor(72, 114, 255)
            };

            using var textPaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.White
            };

            using var font = new SKFont(SKTypeface.Default, 14 * DpiScale);

            var rect = new SKRect(LayoutRect.Left, LayoutRect.Top, LayoutRect.Right, LayoutRect.Bottom);
            canvas.DrawRoundRect(rect, 8 * DpiScale, 8 * DpiScale, backgroundPaint);

            font.MeasureText(_text, out var textBounds);
            float x = rect.MidX - textBounds.MidX;
            float y = rect.MidY - textBounds.MidY;

            canvas.DrawText(_text, x, y, font, textPaint);
        }
    }
}
