using System.Drawing;

using Kx.Core.Event;
using Kx.Sdk.Events;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.State;
using Kx.Sdk.WindowHost;
using Kx.UI.Commands;
using Kx.UI.Manager;
using Kx.UI.State;

using SkiaSharp;

namespace Kx.Tests;

public sealed class ButtonStateImageTests {
    [Fact]
    public void WhenNormalStateImageIsConfiguredThenButtonDrawsThatImage() {
        using var button = CreateButton();
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(canvas);

        Assert.Equal(SKColors.Red, bitmap.GetPixel(20, 20));
    }

    [Fact]
    public void WhenHoveredThenButtonDrawsHoverImage() {
        using var button = CreateButton();
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));
        button.OnMouseMove(new Point(20, 20));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(canvas);

        Assert.Equal(SKColors.Green, bitmap.GetPixel(20, 20));
    }

    [Fact]
    public void WhenPressedThenButtonDrawsPressedImage() {
        using var button = CreateButton();
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));
        button.OnMouseMove(new Point(20, 20));
        button.OnMouseDown(new Point(20, 20));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(canvas);

        Assert.Equal(SKColors.Blue, bitmap.GetPixel(20, 20));
    }

    [Fact]
    public void WhenButtonUsesFixedBoundsThenArrangeKeepsConfiguredSize() {
        using var button = new Kx.UI.Elements.Button(new TestVisualContext(), "button", string.Empty) {
            FixedBounds = new Rectangle(-34, 16, 17, 17)
        };

        button.Arrange(new Rectangle(0, 0, 400, 300), 1f);

        Assert.Equal(new Rectangle(349, 16, 17, 17), button.Bounds);
    }

    private static Kx.UI.Elements.Button CreateButton() {
        var button = new Kx.UI.Elements.Button(new TestVisualContext(), "button", string.Empty);
        button.Arrange(new Rectangle(0, 0, 40, 40), 1f);
        return button;
    }

    private static SKBitmap CreateSolidBitmap(SKColor color) {
        var bitmap = new SKBitmap(4, 4);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        return bitmap;
    }

    private static SKSurface CreateSurface(out SKCanvas canvas, out SKBitmap bitmap) {
        bitmap = new SKBitmap(40, 40);
        var surface = SKSurface.Create(new SKImageInfo(40, 40), bitmap.GetPixels(), bitmap.RowBytes);
        canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        return surface;
    }

    private sealed class TestVisualContext : IVisualContext {
        public float DpiScale => 1f;
        public IUiDispatcher UiThread { get; } = new ImmediateDispatcher();
        public IUIElementManager UIElementManager { get; } = new UIElementManager();
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
}
