using System.Drawing;

using Kx.Core.Event;
using Kx.Sdk.Events;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.UI.State;
using Kx.Sdk.WindowHost;
using Kx.UI.Commands;
using Kx.UI.Manager;
using Kx.UI.State;

using SkiaSharp;

namespace Kx.Tests;

public sealed class DebugOverlayTests {
    [Fact]
    public void WhenContentRectOverlayIsToggledThenContentRectStateIsTracked() {
        using var scope = new DebugOverlayScope();

        DebugOverlay.Toggle(DebugOverlay.OverlayType.ContentRect);

        Assert.True(DebugOverlay.Enabled);
        Assert.True(DebugOverlay.ShowContentRect);
        Assert.True(DebugOverlay.IsOn(DebugOverlay.OverlayType.ContentRect));
        Assert.False(DebugOverlay.ShowOnlyHoveredElement);
    }

    [Fact]
    public void WhenLayoutPresetIsAppliedThenExpectedOverlayFlagsAreEnabled() {
        using var scope = new DebugOverlayScope();

        DebugOverlay.ApplyPreset(DebugOverlay.OverlayPreset.Layout);

        Assert.True(DebugOverlay.Enabled);
        Assert.True(DebugOverlay.ShowBounds);
        Assert.True(DebugOverlay.ShowLayoutRect);
        Assert.True(DebugOverlay.ShowContentRect);
        Assert.False(DebugOverlay.ShowMeta);
        Assert.False(DebugOverlay.ShowParentChain);
        Assert.False(DebugOverlay.ShowOnlyHoveredElement);
        Assert.Equal(DebugOverlay.OverlayPreset.Layout, DebugOverlay.GetCurrentPreset());
    }

    [Fact]
    public void WhenPresetIsCycledThenTheNextPresetIsApplied() {
        using var scope = new DebugOverlayScope();

        DebugOverlay.ApplyPreset(DebugOverlay.OverlayPreset.Layout);
        DebugOverlay.CyclePreset();

        Assert.Equal(DebugOverlay.OverlayPreset.Hierarchy, DebugOverlay.GetCurrentPreset());
    }

    [Fact]
    public void WhenContentRectOverlayIsEnabledThenItDrawsTheContentOutline() {
        using var scope = new DebugOverlayScope();
        var context = new TestVisualContext();
        using var element = new TestElement(context, "element") {
            Padding = new Kx.Sdk.UI.Layout.Thickness(4),
            FixedBounds = new Rectangle(0, 0, 30, 30)
        };
        element.Arrange(new Rectangle(0, 0, 30, 30), 1f);

        DebugOverlay.Enabled = true;
        DebugOverlay.ShowContentRect = true;
        DebugOverlay.ContentColor = SKColors.Magenta;

        using var bitmap = Render(element, 30, 30);

        Assert.True(HasColor(bitmap.GetPixel(10, 4), SKColors.Magenta));
    }

    [Fact]
    public void WhenHoveredOnlyIsEnabledThenOnlyTheHoveredElementDrawsItsOverlay() {
        using var scope = new DebugOverlayScope();
        var context = new TestVisualContext();
        using var first = new TestElement(context, "first") { FixedBounds = new Rectangle(0, 0, 20, 20) };
        using var second = new TestElement(context, "second") { FixedBounds = new Rectangle(20, 0, 20, 20) };
        first.Arrange(new Rectangle(0, 0, 40, 20), 1f);
        second.Arrange(new Rectangle(0, 0, 40, 20), 1f);
        context.UIElementManager.Add(first);
        context.UIElementManager.Add(second);
        context.UIElementManager.MouseMove(new Point(25, 10));

        DebugOverlay.Enabled = true;
        DebugOverlay.ShowContentRect = true;
        DebugOverlay.ShowOnlyHoveredElement = true;
        DebugOverlay.ContentColor = SKColors.Cyan;

        using var firstBitmap = Render(first, 40, 20);
        using var secondBitmap = Render(second, 40, 20);

        Assert.False(ContainsColor(firstBitmap, SKColors.Cyan));
        Assert.True(ContainsColor(secondBitmap, SKColors.Cyan));
    }

    private static SKBitmap Render(UIElement element, int width, int height) {
        var bitmap = new SKBitmap(width, height);
        using var surface = SKSurface.Create(new SKImageInfo(width, height), bitmap.GetPixels(), bitmap.RowBytes);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        element.Draw(canvas);
        canvas.Flush();
        return bitmap;
    }

    private static bool ContainsColor(SKBitmap bitmap, SKColor color) {
        for (int y = 0; y < bitmap.Height; y++) {
            for (int x = 0; x < bitmap.Width; x++) {
                if (HasColor(bitmap.GetPixel(x, y), color))
                    return true;
            }
        }

        return false;
    }

    private static bool HasColor(SKColor actual, SKColor expected) {
        return actual.Alpha > 0 &&
               actual.Red == expected.Red &&
               actual.Green == expected.Green &&
               actual.Blue == expected.Blue;
    }

    private sealed class TestElement(IVisualContext context, string id) : UIElement(context, id) {
        protected override void OnDraw(SKCanvas canvas) {
        }
    }

    private sealed class TestVisualContext : IVisualContext {
        public float DpiScale => 1f;
        public IUiDispatcher UiThread { get; } = new ImmediateDispatcher();
        public UIElementManager UIElementManager { get; } = new();
        IUIElementManager IVisualContext.UIElementManager => UIElementManager;
        public IEventManager Events { get; } = new EventManager();
        public IUiCommandRegistry Commands { get; } = new UiCommandRegistry();
        public IUiStateStore State { get; } = new UiStateStore();

        public void RequestRender() {
        }

        public void CloseWindow() {
        }

        public void OpenWindow(string name) {
        }
    }

    private sealed class ImmediateDispatcher : IUiDispatcher {
        public bool InvokeRequired => false;

        public void BeginInvoke(Delegate d) {
            ArgumentNullException.ThrowIfNull(d);
            d.DynamicInvoke();
        }

        public void Invoke(Action action) {
            ArgumentNullException.ThrowIfNull(action);
            action();
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
        private readonly float _fontSize = DebugOverlay.FontSize;
        private readonly float _itemSpacing = DebugOverlay.ItemSpacing;
        private readonly float _itemPadding = DebugOverlay.ItemPadding;
        private readonly int _maxParentItems = DebugOverlay.MaxParentItems;
        private readonly SKColor _boundsColor = DebugOverlay.BoundsColor;
        private readonly SKColor _layoutColor = DebugOverlay.LayoutColor;
        private readonly SKColor _contentColor = DebugOverlay.ContentColor;
        private readonly SKColor _textBgColor = DebugOverlay.TextBgColor;
        private readonly SKColor _textColor = DebugOverlay.TextColor;

        public void Dispose() {
            DebugOverlay.Enabled = _enabled;
            DebugOverlay.ShowBounds = _showBounds;
            DebugOverlay.ShowLayoutRect = _showLayoutRect;
            DebugOverlay.ShowMeta = _showMeta;
            DebugOverlay.ShowParentChain = _showParentChain;
            DebugOverlay.ShowContentRect = _showContentRect;
            DebugOverlay.ShowOnlyHoveredElement = _showOnlyHoveredElement;
            DebugOverlay.FontSize = _fontSize;
            DebugOverlay.ItemSpacing = _itemSpacing;
            DebugOverlay.ItemPadding = _itemPadding;
            DebugOverlay.MaxParentItems = _maxParentItems;
            DebugOverlay.BoundsColor = _boundsColor;
            DebugOverlay.LayoutColor = _layoutColor;
            DebugOverlay.ContentColor = _contentColor;
            DebugOverlay.TextBgColor = _textBgColor;
            DebugOverlay.TextColor = _textColor;
        }
    }
}
