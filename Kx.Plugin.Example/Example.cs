// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Abstractions.Plugin;
using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Actions;
using Kx.Abstractions.UI.Commands;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.Payloads;
using Kx.Abstractions.UI.State;
using Kx.Abstractions.UI.Themes;

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
        themeRegistry.Register("Example.Dark", new WindowTheme {
            Frame = new FrameConfig {
                Style = FrameStyle.Default,
                Default = new DefaultFrameConfig {
                    Title = "Example Plugin Window",
                    BackgroundColor = "#1B1D22",
                    TitleBarColor = "#262A33",
                    BorderColor = "#4C7FFF",
                    SeparatorColor = "#394052",
                    TitleColor = "#FFD166"
                }
            },
            Controls = [
                new ControlConfig {
                    Type = "StackPanel",
                    Id = "example_panel",
                    Layer = "Content",
                    Padding = new ThicknessConfig {
                        Left = 12,
                        Top = 12,
                        Right = 12,
                        Bottom = 12
                    },
                    Bounds = new BoundsConfig {
                        X = 24,
                        Y = 24,
                        Width = 220,
                        Height = 360
                    },
                    Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                        ["orientation"] = "Vertical",
                        ["spacing"] = "8"
                    },
                    OrientationBinding = PanelOrientationState.Path,
                    SpacingBinding = PanelSpacingState.Path,
                    Children = [
                        new ControlConfig {
                            Type = "Label",
                            Id = "example_title",
                            Text = "Plugin Window",
                            TextBinding = TitleState.Path + "|trim|upper|prefix:[BOUND] ",
                            Color = "#F5F5F5",
                            ColorBinding = TitleColorState.Path,
                            FontSizeBinding = TitleFontSizeState.Path,
                            Font = new FontConfig {
                                Name = "Segoe UI",
                                Size = 16,
                                Style = "Bold"
                            }
                        },
                        new ControlConfig {
                            Type = "ExampleBadge",
                            Id = "example_badge",
                            Text = "Plugin Content",
                            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                                ["textState"] = BadgeTextState.Path
                            }
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_toggle_button",
                            Text = "Toggle Badge",
                            FontSizeBinding = ButtonFontSizeState.Path,
                            OnClick = "example.toggleVisibility:id:example_badge"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_hide_button",
                            Text = "Hide Badge",
                            OnClick = "hide:id:example_badge"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_show_button",
                            Text = "Show Badge",
                            OnClick = "show:id:example_badge"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_set_text_button",
                            Text = "Rename Title",
                            OnClick = "setText:id:example_title|Updated by markup"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_set_color_button",
                            Text = "Highlight Title",
                            OnClick = "setColor:{\"targetId\":\"id:example_title\",\"color\":\"#FFD166\"}"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_disable_toggle_button",
                            Text = "Disable Toggle",
                            OnClick = "disable:{\"targetId\":\"id:example_toggle_button\",\"enabled\":false}"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_enable_toggle_button",
                            Text = "Enable Toggle",
                            OnClick = "enable:id:example_toggle_button"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_focus_toggle_button",
                            Text = "Focus Toggle",
                            OnClick = "focus:id:example_toggle_button"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_command_button",
                            Text = "Rename via Command",
                            OnClick = "runCommand:example.renameBadge|{\"targetId\":\"example_badge\",\"text\":\"Updated by command\"}"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_open_window_button",
                            Text = "Open Alternate",
                            OnClick = "openWindow:Example.Alternate"
                        },
                        new ControlConfig {
                            Type = "Label",
                            Id = "example_hidden_hint",
                            Text = "Shown when merge hint is hidden",
                            Color = "#D9D9D9",
                            VisibleBinding = MergeHintVisibleState.Path + "|not"
                        }
                    ]
                }
            ]
        });

        themeRegistry.Register("Example.Alternate", new WindowTheme {
            Frame = new FrameConfig {
                Style = FrameStyle.Default,
                Default = new DefaultFrameConfig {
                    Title = "Alternate Plugin Window",
                    BackgroundColor = "#151821",
                    TitleBarColor = "#212634",
                    BorderColor = "#77C17A",
                    SeparatorColor = "#394052"
                }
            },
            Controls = [
                new ControlConfig {
                    Type = "StackPanel",
                    Id = "alternate_panel",
                    Layer = "Content",
                    Padding = new ThicknessConfig {
                        Left = 12,
                        Top = 12,
                        Right = 12,
                        Bottom = 12
                    },
                    Bounds = new BoundsConfig {
                        X = 24,
                        Y = 24,
                        Width = 240,
                        Height = 160
                    },
                    Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                        ["orientation"] = "Vertical",
                        ["spacing"] = "8"
                    },
                    Children = [
                        new ControlConfig {
                            Type = "Label",
                            Id = "alternate_title",
                            Text = "Alternate View",
                            Color = "#E9FFE9",
                            Font = new FontConfig {
                                Name = "Segoe UI",
                                Size = 16,
                                Style = "Bold"
                            }
                        },
                        new ControlConfig {
                            Type = "ExampleBadge",
                            Id = "alternate_badge",
                            Text = "Second Window"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "alternate_back_button",
                            Text = "Back",
                            OnClick = "openWindow:MainWindow"
                        }
                    ]
                }
            ]
        });

        windowRegistry.Register("MainWindow", new WindowConfig {
            Theme = "Example.Dark",
            Frame = new FrameConfig {
                Default = new DefaultFrameConfig {
                    Title = "Merged Main Window",
                    BorderColor = "#FF8C42",
                    TitleColor = "#F5F5F5"
                }
            },
            Controls = [
                new ControlConfig {
                    Id = "example_panel",
                    Children = [
                        new ControlConfig {
                            Id = "example_title",
                            Text = "Plugin Window (merged)",
                            Color = "#FFD166"
                        },
                        new ControlConfig {
                            Type = "Label",
                            Id = "example_merge_hint",
                            Text = "Added by WindowConfig merge",
                            Color = "#8BD3FF",
                            VisibleBinding = MergeHintVisibleState.Path
                        }
                    ]
                }
            ]
        });

        windowRegistry.Register("Example.Alternate", new WindowConfig {
            Theme = "Example.Alternate"
        });

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
