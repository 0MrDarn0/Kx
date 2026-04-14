using Kx.App;
using Kx.Core.Event;
using Kx.Sdk.Events;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI.Layout;
using Kx.Tests.TestInfrastructure;

namespace Kx.Tests;

public sealed class WindowInteractionOverlayHotkeyTests {
    [Fact]
    public void WhenCtrlShiftD1IsPressedThenLayoutOverlayPresetIsApplied() {
        using var scope = new DebugOverlayScope();
        var host = new TestWindowHost();
        using var context = new WindowContext(host, host, host, new EventManager());
        using var renderer = new TestWindowRenderer();
        context.SetRenderer(renderer);
        _ = new WindowInteraction(host, context);

        host.RaiseKeyDown(KeyCode.Control);
        host.RaiseKeyDown(KeyCode.Shift);
        host.RaiseKeyDown(KeyCode.D1);

        Assert.Equal(DebugOverlay.OverlayPreset.Layout, DebugOverlay.GetCurrentPreset());
        Assert.Equal(1, renderer.RequestRenderCallCount);
    }

    [Fact]
    public void WhenCtrlShiftDIsPressedThenOverlayPresetCycles() {
        using var scope = new DebugOverlayScope();
        var host = new TestWindowHost();
        using var context = new WindowContext(host, host, host, new EventManager());
        using var renderer = new TestWindowRenderer();
        context.SetRenderer(renderer);
        _ = new WindowInteraction(host, context);

        host.RaiseKeyDown(KeyCode.Control);
        host.RaiseKeyDown(KeyCode.Shift);
        host.RaiseKeyDown(KeyCode.D);

        Assert.Equal(DebugOverlay.OverlayPreset.Layout, DebugOverlay.GetCurrentPreset());
        Assert.Equal(1, renderer.RequestRenderCallCount);
    }

    private sealed class TestWindowRenderer : IWindowRenderer {
        public int RequestRenderCallCount { get; private set; }
        public long LastRenderDurationMs => 0;
        public int LastPresentError => 0;

        public void ToggleDebugOverlay() {
        }

        public void RequestRender() {
            RequestRenderCallCount++;
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

    private sealed class DebugOverlayScope : IDisposable {
        private readonly bool _enabled = DebugOverlay.Enabled;
        private readonly bool _showBounds = DebugOverlay.ShowBounds;
        private readonly bool _showLayoutRect = DebugOverlay.ShowLayoutRect;
        private readonly bool _showMeta = DebugOverlay.ShowMeta;
        private readonly bool _showParentChain = DebugOverlay.ShowParentChain;
        private readonly bool _showContentRect = DebugOverlay.ShowContentRect;
        private readonly bool _showOnlyHoveredElement = DebugOverlay.ShowOnlyHoveredElement;

        public void Dispose() {
            DebugOverlay.Enabled = _enabled;
            DebugOverlay.ShowBounds = _showBounds;
            DebugOverlay.ShowLayoutRect = _showLayoutRect;
            DebugOverlay.ShowMeta = _showMeta;
            DebugOverlay.ShowParentChain = _showParentChain;
            DebugOverlay.ShowContentRect = _showContentRect;
            DebugOverlay.ShowOnlyHoveredElement = _showOnlyHoveredElement;
        }
    }
}
