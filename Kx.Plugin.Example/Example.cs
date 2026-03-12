// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Abstractions.Plugin;
using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Actions;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.Themes;

using SkiaSharp;

namespace Kx.Plugin;

public sealed class Example : IPlugin {
    public string Name => "Example";

    public void Initialize(IPluginContext context) {
        var controlRegistry = context.Services.Get<IControlRegistry>();
        var actionRegistry = context.Services.Get<IMarkupActionRegistry>();
        var themeRegistry = context.Services.Get<IThemeRegistry>();
        var windowRegistry = context.Services.Get<IWindowRegistry>();

        controlRegistry.Register("ExampleBadge", (uiContext, config) => new ExampleBadge(uiContext, config.Id, config.Text));
        actionRegistry.Register("example.toggleVisibility", actionContext => ToggleVisibility(actionContext));
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
                        Height = 120
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
                            OnClick = "example.toggleVisibility:example_badge"
                        }
                    ]
                }
            ]
        });

        windowRegistry.Register("MainWindow", new WindowConfig {
            Theme = "Example.Dark"
        });

        context.Logger.Info($"{Name} initialized");
        context.Logger.Info($"ApiVersion: {context.ApiVersion}");
    }

    public void Dispose() {

    }

    private static void ToggleVisibility(IMarkupActionContext actionContext) {
        if (string.IsNullOrWhiteSpace(actionContext.Argument))
            return;

        if (!actionContext.VisualContext.UIElementManager.TryGet(actionContext.Argument, out var visual) || visual is null)
            return;

        visual.Visible = !visual.Visible;
    }

    private sealed class ExampleBadge(IVisualContext context, string id, string? text) : UIElement(context, id) {
        private readonly string _text = string.IsNullOrWhiteSpace(text) ? "Example" : text;

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
