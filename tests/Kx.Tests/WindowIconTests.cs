using Kx.App;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;
using Kx.Sdk.WindowHost;
using Kx.Tests.TestInfrastructure;
using Kx.Utility;

namespace Kx.Tests;

public sealed class WindowIconTests {
    [Fact]
    public void WhenWindowFrameDefinesIconThenHostReceivesThatIcon() {
        CreateIconResource("frame.ico", SystemIcons.Application);
        var composition = new RuntimeUiComposition();
        var windowHost = new TestWindowHost();
        composition.WindowRegistry.Register(nameof(IconWindow), new WindowConfig {
            Frame = new FrameConfig {
                Default = new DefaultFrameConfig {
                    Icon = "Icons:WindowIconTests:frame.ico"
                }
            }
        });

        using var window = new IconWindow(
            windowHost,
            composition.ActionRegistry,
            composition.ControlRegistry,
            composition.ThemeRegistry,
            composition.WindowRegistry);

        Assert.Equal(File.ReadAllBytes(Paths.GetResource("Icons\\WindowIconTests\\frame.ico")), windowHost.LastWindowIconBytes);
    }

    [Fact]
    public void WhenWindowOverridesIconInCodeThenCodeIconWins() {
        CreateIconResource("frame-override.ico", SystemIcons.Application);
        CreateIconResource("code-override.ico", SystemIcons.Warning);
        var composition = new RuntimeUiComposition();
        var windowHost = new TestWindowHost();
        composition.WindowRegistry.Register(nameof(CodeIconWindow), new WindowConfig {
            Frame = new FrameConfig {
                Default = new DefaultFrameConfig {
                    Icon = "Icons:WindowIconTests:frame-override.ico"
                }
            }
        });

        using var window = new CodeIconWindow(
            windowHost,
            composition.ActionRegistry,
            composition.ControlRegistry,
            composition.ThemeRegistry,
            composition.WindowRegistry);

        Assert.Equal(File.ReadAllBytes(Paths.GetResource("Icons\\WindowIconTests\\code-override.ico")), windowHost.LastWindowIconBytes);
    }

    private static void CreateIconResource(string fileName, Icon icon) {
        var relativePath = Path.Combine("Icons", "WindowIconTests", fileName);
        var fullPath = Paths.GetResource(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var stream = File.Create(fullPath);
        icon.Save(stream);
    }

    private class IconWindow : Window {
        public IconWindow(
            IWindowHost host,
            IMarkupActionRegistry actionRegistry,
            IControlRegistry controlRegistry,
            IThemeRegistry themeRegistry,
            IWindowRegistry windowRegistry)
            : base(host, null, null, actionRegistry, null, null, controlRegistry, themeRegistry, windowRegistry) {
        }

        protected override void InitializeRenderer() {
            _ctx.SetRenderer(new NullWindowRenderer());
        }

        protected override void InitializeInteraction() {
        }

        protected override void RegisterWindowEvents() {
        }

        protected override string WindowDefinitionName => nameof(IconWindow);

        public override void Dispose() {
            _ctx.Dispose();
        }
    }

    private sealed class CodeIconWindow : IconWindow {
        public CodeIconWindow(
            IWindowHost host,
            IMarkupActionRegistry actionRegistry,
            IControlRegistry controlRegistry,
            IThemeRegistry themeRegistry,
            IWindowRegistry windowRegistry)
            : base(host, actionRegistry, controlRegistry, themeRegistry, windowRegistry) {
        }

        protected override string WindowDefinitionName => nameof(CodeIconWindow);
        protected override string? WindowIconResource => "Icons:WindowIconTests:code-override.ico";
    }

    private sealed class NullWindowRenderer : IWindowRenderer {
        public long LastRenderDurationMs => 0;
        public int LastPresentError => 0;

        public void ToggleDebugOverlay() {
        }

        public void RequestRender() {
        }

        public void Resize(int width, int height) {
        }

        public void TogglePerfOverlay() {
        }

        public void ToggleContentRectDebug() {
        }

        public void Dispose() {
        }
    }
}
