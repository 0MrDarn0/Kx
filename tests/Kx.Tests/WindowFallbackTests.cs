using Kx.App;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;
using Kx.Sdk.WindowHost;
using Kx.Tests.TestInfrastructure;

using System.Reflection;

namespace Kx.Tests;

public sealed class WindowFallbackTests {
    [Fact]
    public void WhenWindowHasNoConfiguredControlsThenFallbackUiRuns() {
        var composition = new RuntimeUiComposition();
        var windowHost = new TestWindowHost();
        composition.WindowContentRegistry.Register(nameof(FallbackWindow), new WindowContentDefinition());

        using var window = new FallbackWindow(
            windowHost,
            composition.ActionRegistry,
            composition.ControlRegistry,
            composition.WindowFrameRegistry,
            composition.WindowContentRegistry);

        InitializeWindow(window);

        Assert.True(window.FallbackBuilt);
    }

    [Fact]
    public void WhenWindowHasConfiguredOverlayControlsThenFallbackUiDoesNotRun() {
        var composition = new RuntimeUiComposition();
        var windowHost = new TestWindowHost();
        composition.WindowContentRegistry.Register(nameof(FallbackWindow), new WindowContentDefinition {
            Controls = [
                new ControlConfig {
                    Type = "Label",
                    Id = "overlay-title",
                    Text = "Configured",
                    Layer = "Overlay"
                }
            ]
        });

        using var window = new FallbackWindow(
            windowHost,
            composition.ActionRegistry,
            composition.ControlRegistry,
            composition.WindowFrameRegistry,
            composition.WindowContentRegistry);

        InitializeWindow(window);

        Assert.False(window.FallbackBuilt);
        Assert.True(window.ExposedHasConfiguredControls);
        Assert.False(window.ExposedHasConfiguredContentControls);
    }

    private static void InitializeWindow(Window window) {
        ArgumentNullException.ThrowIfNull(window);

        MethodInfo? initializeWindowMethod = typeof(Window).GetMethod("InitializeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(initializeWindowMethod);
        initializeWindowMethod.Invoke(window, null);
    }

    private sealed class FallbackWindow : Window {
        public bool FallbackBuilt { get; private set; }
        public bool ExposedHasConfiguredControls => HasConfiguredControls;
        public bool ExposedHasConfiguredContentControls => HasConfiguredContentControls;

        public FallbackWindow(
            IWindowHost host,
            IMarkupActionRegistry actionRegistry,
            IControlRegistry controlRegistry,
            IWindowFrameRegistry windowFrameRegistry,
            IWindowContentRegistry windowContentRegistry)
            : base(host, null, null, actionRegistry, null, null, controlRegistry, windowFrameRegistry, windowContentRegistry) {
        }

        protected override void OnInitialize() {
            if (HasConfiguredControls)
                return;

            FallbackBuilt = true;
        }

        protected override void InitializeRenderer() {
            _ctx.SetRenderer(new NullWindowRenderer());
        }

        protected override void InitializeInteraction() {
        }

        protected override void RegisterWindowEvents() {
        }

        protected override string WindowContentDefinitionName => nameof(FallbackWindow);

        public override void Dispose() {
            _ctx.Dispose();
        }
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
