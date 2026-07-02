// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Sdk.Plugin;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Payloads;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.Themes;

namespace Kx.Plugin;

public sealed class Example : IPlugin {
    private static readonly UiStateKey<string> _titleState = new("example.title");
    private static readonly UiStateKey<string> _titleColorState = new("example.titleColor");
    private static readonly UiStateKey<float> _titleFontSizeState = new("example.titleFontSize");
    private static readonly UiStateKey<string> _badgeTextState = new("example.badgeText");
    private static readonly UiStateKey<bool> _mergeHintVisibleState = new("example.mergeHintVisible");
    private static readonly UiStateKey<float> _panelSpacingState = new("example.panelSpacing");
    private static readonly UiStateKey<string> _panelOrientationState = new("example.panelOrientation");
    private static readonly UiStateKey<float> _buttonFontSizeState = new("example.buttonFontSize");

    public string Name => "Example";

    public void Initialize(IPluginContext context) {
        var controlRegistry = context.Services.Get<IControlRegistry>();
        var actionRegistry = context.Services.Get<IMarkupActionRegistry>();
        var commandRegistry = context.Services.Get<IUiCommandRegistry>();
        var stateStore = context.Services.Get<IUiStateStore>();
        var frameRegistry = context.Services.Get<IWindowFrameRegistry>();
        var contentRegistry = context.Services.Get<IWindowContentRegistry>();

        stateStore.Set(_titleState, "  Plugin Window (bound)  ");
        stateStore.Set(_titleColorState, "#F5F5F5");
        stateStore.Set(_titleFontSizeState, 16f);
        stateStore.Set(_badgeTextState, "Plugin Content");
        stateStore.Set(_mergeHintVisibleState, true);
        stateStore.Set(_panelSpacingState, 8f);
        stateStore.Set(_panelOrientationState, "Vertical");
        stateStore.Set(_buttonFontSizeState, 14f);

        controlRegistry.Register("ExampleBadge", (uiContext, config) => new ExampleBadge(uiContext, config.Id, config.Text, config.Properties.TryGetValue("textState", out var textState) ? textState : null));
        actionRegistry.Register("example.toggleVisibility", ToggleVisibility);
        commandRegistry.Register("example.renameBadge", commandContext => RenameBadge(stateStore, commandContext));
        RegisterFrameDefinition(frameRegistry, "Example.Dark", "Frames", "example_dark_frame.yaml");
        RegisterFrameDefinition(frameRegistry, "Example.Alternate", "Frames", "example_alternate_frame.yaml");

        RegisterWindowContentDefinition(contentRegistry, "MainWindow", "Content", "main_window_content.yaml");
        RegisterWindowContentDefinition(contentRegistry, "Example.Alternate", "Content", "example_alternate_content.yaml");

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

        stateStore.Set(_badgeTextState, payload.Text);
    }

    private static void RegisterFrameDefinition(IWindowFrameRegistry frameRegistry, string name, params string[] relativePathSegments) {
        ArgumentNullException.ThrowIfNull(frameRegistry);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        frameRegistry.Register(name, MarkupYamlLoader.Load<WindowFrameDefinition>(GetMarkupPath(relativePathSegments)));
    }

    private static void RegisterWindowContentDefinition(IWindowContentRegistry contentRegistry, string name, params string[] relativePathSegments) {
        ArgumentNullException.ThrowIfNull(contentRegistry);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        contentRegistry.Register(name, MarkupYamlLoader.Load<WindowContentDefinition>(GetMarkupPath(relativePathSegments)));
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

        public ExampleBadge(IVisualContext context, string id, string? text, string? textStatePath)
            : base(context, id) {
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

        protected override void OnDraw(IKxCanvas canvas) {
            float left = LayoutRect.Left;
            float top = LayoutRect.Top;
            float right = LayoutRect.Right;
            float bottom = LayoutRect.Bottom;
            float radius = 8 * DpiScale;
            float fontSize = 14 * DpiScale;

            canvas.DrawRoundedRect(left, top, right, bottom, radius, radius, new KxColor(72, 114, 255));

            canvas.MeasureText(_text, fontSize, out float textWidth, out float textHeight);

            float centerX = left + ((right - left) / 2f);
            float centerY = top + ((bottom - top) / 2f);
            float x = centerX - (textWidth / 2f);
            float y = centerY + (textHeight / 2f);

            canvas.DrawText(_text, x, y, fontSize, KxColor.Parse("#FFFFFF"));
        }
    }
}
