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
using Kx.Abstractions.UI.Themes;

using SkiaSharp;

namespace Kx.Plugin;

public sealed class Example : IPlugin {
    public string Name => "Example";

    public void Initialize(IPluginContext context) {
        var controlRegistry = context.Services.Get<IControlRegistry>();
        var actionRegistry = context.Services.Get<IMarkupActionRegistry>();
        var commandRegistry = context.Services.Get<IUiCommandRegistry>();
        var themeRegistry = context.Services.Get<IThemeRegistry>();
        var windowRegistry = context.Services.Get<IWindowRegistry>();

        controlRegistry.Register("ExampleBadge", (uiContext, config) => new ExampleBadge(uiContext, config.Id, config.Text));
        actionRegistry.Register("example.toggleVisibility", actionContext => ToggleVisibility(actionContext));
        commandRegistry.Register("example.renameBadge", commandContext => RenameBadge(commandContext));
        themeRegistry.Register("Example.Dark", new WindowTheme {
            Frame = new FrameConfig {
                Style = FrameStyle.Default,
                Default = new DefaultFrameConfig {
                    Title = "Example Plugin Window",
                    BackgroundColor = "#1B1D22",
                    TitleBarColor = "#262A33",
                    BorderColor = "#4C7FFF",
                    SeparatorColor = "#394052"
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
                    Children = [
                        new ControlConfig {
                            Type = "Label",
                            Id = "example_title",
                            Text = "Plugin Window",
                            Color = "#F5F5F5",
                            Font = new FontConfig {
                                Name = "Segoe UI",
                                Size = 16,
                                Style = "Bold"
                            }
                        },
                        new ControlConfig {
                            Type = "ExampleBadge",
                            Id = "example_badge",
                            Text = "Plugin Content"
                        },
                        new ControlConfig {
                            Type = "Button",
                            Id = "example_toggle_button",
                            Text = "Toggle Badge",
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
                    BorderColor = "#FF8C42"
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
                            Color = "#8BD3FF"
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

    private static void RenameBadge(IUiCommandContext commandContext) {
        if (!commandContext.Payload.TryDeserialize<TextUpdatePayload>(out var payload) || payload is null)
            return;

        if (!commandContext.VisualContext.UIElementManager.TryGet(payload.TargetId, out var visual) || visual is not ExampleBadge badge)
            return;

        badge.SetText(payload.Text);
    }

    private sealed class ExampleBadge(IVisualContext context, string id, string? text) : UIElement(context, id) {
        private string _text = string.IsNullOrWhiteSpace(text) ? "Example" : text;

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
