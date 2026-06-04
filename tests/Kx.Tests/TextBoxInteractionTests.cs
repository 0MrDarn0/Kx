using System.Drawing;
using System.Text;

using Kx.Core.Event;
using Kx.Sdk.Events;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.State;
using Kx.Sdk.WindowHost;
using Kx.UI.Commands;
using Kx.UI.Manager;
using Kx.UI.State;

using SkiaSharp;

namespace Kx.Tests;

public sealed class TextBoxInteractionTests {
    [Fact]
    public void WhenScrollMarkerIsDraggedOutsideTextBoxThenDragStillUpdatesMarkerPosition() {
        var context = new TestVisualContext();
        using var textBox = CreateScrollableTextBox(context);
        context.UIElementManager.Add(textBox);

        Render(textBox, out var bitmapBeforeDrag);
        Rectangle markerBoundsBeforeDrag = FindScrollMarkerBounds(bitmapBeforeDrag);
        Point dragStart = GetMarkerCenter(markerBoundsBeforeDrag);

        context.UIElementManager.MouseDown(dragStart);
        context.UIElementManager.MouseMove(new Point(dragStart.X, 140));

        Render(textBox, out var bitmapAfterDrag);
        Rectangle markerBoundsAfterDrag = FindScrollMarkerBounds(bitmapAfterDrag);

        Assert.True(markerBoundsAfterDrag.Top > markerBoundsBeforeDrag.Top);
    }

    [Fact]
    public void WhenScrollMarkerIsReleasedOutsideTextBoxThenFurtherMovesDoNotKeepDragging() {
        var context = new TestVisualContext();
        using var textBox = CreateScrollableTextBox(context);
        context.UIElementManager.Add(textBox);

        Render(textBox, out var bitmapBeforeDrag);
        Point dragStart = GetMarkerCenter(FindScrollMarkerBounds(bitmapBeforeDrag));

        context.UIElementManager.MouseDown(dragStart);
        context.UIElementManager.MouseMove(new Point(dragStart.X, 140));
        Render(textBox, out var bitmapAfterDrag);
        int markerTopAfterDrag = FindScrollMarkerBounds(bitmapAfterDrag).Top;

        context.UIElementManager.MouseUp(new Point(dragStart.X, 140));
        context.UIElementManager.MouseMove(new Point(dragStart.X, 20));
        Render(textBox, out var bitmapAfterRelease);
        int markerTopAfterRelease = FindScrollMarkerBounds(bitmapAfterRelease).Top;

        Assert.Equal(markerTopAfterDrag, markerTopAfterRelease);
    }

    private static Kx.UI.Elements.TextBox CreateScrollableTextBox(TestVisualContext context) {
        var textBox = new Kx.UI.Elements.TextBox(context, "textBox", CreateLongText()) {
            BackgroundColor = SKColors.Black,
            BorderThickness = 0f,
            ScrollBarColor = SKColors.Magenta,
            FixedBounds = new Rectangle(0, 0, 100, 80)
        };

        textBox.Arrange(new Rectangle(0, 0, 100, 80), 1f);
        Render(textBox, out _);
        return textBox;
    }

    private static string CreateLongText() {
        var builder = new StringBuilder();
        for (int i = 0; i < 40; i++) {
            builder.Append("Scrollable line ");
            builder.Append(i);
            builder.Append(' ');
            builder.Append('x', 8);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void Render(Kx.UI.Elements.TextBox textBox, out SKBitmap bitmap) {
        bitmap = new SKBitmap(100, 80);
        using var surface = SKSurface.Create(new SKImageInfo(100, 80), bitmap.GetPixels(), bitmap.RowBytes);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        textBox.Draw(new SkiaTestCanvas(canvas));
        canvas.Flush();
    }

    private static Rectangle FindScrollMarkerBounds(SKBitmap bitmap) {
        int left = bitmap.Width;
        int top = bitmap.Height;
        int right = -1;
        int bottom = -1;

        for (int y = 0; y < bitmap.Height; y++) {
            for (int x = 0; x < bitmap.Width; x++) {
                if (bitmap.GetPixel(x, y) != SKColors.Magenta)
                    continue;

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return new Rectangle(left, top, right - left + 1, bottom - top + 1);
    }

    private static Point GetMarkerCenter(Rectangle markerBounds) {
        return new Point(markerBounds.Left + markerBounds.Width / 2, markerBounds.Top + markerBounds.Height / 2);
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

    private sealed class SkiaTestCanvas(SKCanvas canvas) : IKxCanvas {
        private readonly SKCanvas _canvas = canvas;

        public bool TryGetBackend<TBackend>(out TBackend? backend) where TBackend : class {
            if (_canvas is TBackend typedCanvas) {
                backend = typedCanvas;
                return true;
            }

            backend = null;
            return false;
        }
    }
}
