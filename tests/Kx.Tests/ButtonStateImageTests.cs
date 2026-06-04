using System.Drawing;

using Kx.Core.Event;
using Kx.Core.Extensions;
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

public sealed class ButtonStateImageTests {
    [Fact]
    public void WhenNormalStateImageIsConfiguredThenButtonDrawsThatImage() {
        using var button = CreateButton();
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(new SkiaTestCanvas(canvas));

        Assert.Equal(SKColors.Red, bitmap.GetPixel(20, 20));
    }

    [Fact]
    public void WhenHoveredThenButtonDrawsHoverImage() {
        using var button = CreateButton();
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));
        button.OnMouseMove(new Point(20, 20));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(new SkiaTestCanvas(canvas));

        Assert.Equal(SKColors.Green, bitmap.GetPixel(20, 20));
    }

    [Fact]
    public void WhenPressedThenButtonDrawsPressedImage() {
        using var button = CreateButton();
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));
        button.OnMouseMove(new Point(20, 20));
        button.OnMouseDown(new Point(20, 20));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(new SkiaTestCanvas(canvas));

        Assert.Equal(SKColors.Blue, bitmap.GetPixel(20, 20));
    }

    [Fact]
    public void WhenCursorLeavesButtonThenHoverImageIsCleared() {
        var context = new TestVisualContext();
        using var button = CreateButton(context);
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));
        context.UIElementManager.Add(button);

        context.UIElementManager.MouseMove(new Point(20, 20));
        context.UIElementManager.MouseMove(new Point(80, 80));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(new SkiaTestCanvas(canvas));

        Assert.Equal(SKColors.Red, bitmap.GetPixel(20, 20));
    }

    [Fact]
    public void WhenMouseIsReleasedOutsideButtonThenPressedImageIsCleared() {
        var context = new TestVisualContext();
        using var button = CreateButton(context);
        button.SetStateImages(CreateSolidBitmap(SKColors.Red), CreateSolidBitmap(SKColors.Green), CreateSolidBitmap(SKColors.Blue));
        context.UIElementManager.Add(button);

        context.UIElementManager.MouseMove(new Point(20, 20));
        context.UIElementManager.MouseDown(new Point(20, 20));
        context.UIElementManager.MouseMove(new Point(80, 80));
        context.UIElementManager.MouseUp(new Point(80, 80));

        using var surface = CreateSurface(out var canvas, out var bitmap);
        button.Draw(new SkiaTestCanvas(canvas));

        Assert.Equal(SKColors.Red, bitmap.GetPixel(20, 20));
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
        return CreateButton(new TestVisualContext());
    }

    private static Kx.UI.Elements.Button CreateButton(TestVisualContext context) {
        var button = new Kx.UI.Elements.Button(context, "button", string.Empty);
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

    private sealed class SkiaTestCanvas(SKCanvas canvas) : IKxCanvas {
        private readonly SKCanvas _canvas = canvas;

        public void DrawBitmap(object bitmap, float left, float top, float right, float bottom) {
            ArgumentNullException.ThrowIfNull(bitmap);

            if (bitmap is not SKBitmap skBitmap)
                throw new ArgumentException("Unsupported bitmap backend object.", nameof(bitmap));

            _canvas.DrawBitmap(skBitmap, new SKRect(left, top, right, bottom));
        }

        public void DrawRect(float left, float top, float right, float bottom, KxColor color) {
            using var paint = new SKPaint {
                IsAntialias = true,
                Color = color.ToSKColor()
            };

            _canvas.DrawRect(new SKRect(left, top, right, bottom), paint);
        }

        public void DrawRectStroke(float left, float top, float right, float bottom, KxColor color, float thickness = 1f) {
            using var paint = new SKPaint {
                IsAntialias = true,
                Color = color.ToSKColor(),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(0f, thickness)
            };

            _canvas.DrawRect(new SKRect(left, top, right, bottom), paint);
        }

        public void DrawLine(float x0, float y0, float x1, float y1, KxColor color, float thickness = 1f) {
            using var paint = new SKPaint {
                IsAntialias = true,
                Color = color.ToSKColor(),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(0f, thickness)
            };

            _canvas.DrawLine(x0, y0, x1, y1, paint);
        }

        public void DrawRoundedRect(float left, float top, float right, float bottom, float radiusX, float radiusY, KxColor color) {
            using var paint = new SKPaint {
                IsAntialias = true,
                Color = color.ToSKColor()
            };

            _canvas.DrawRoundRect(new SKRect(left, top, right, bottom), radiusX, radiusY, paint);
        }

        public void DrawText(string text, float x, float y, float fontSize, KxColor color, string? fontFamily = null, bool bold = false, bool italic = false, object? font = null) {
            using var paint = new SKPaint {
                IsAntialias = true,
                Color = color.ToSKColor()
            };

            if (font is SKFont skFont) {
                _canvas.DrawText(text, x, y, skFont, paint);
                return;
            }

            var weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            var slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            using var typeface = SKTypeface.FromFamilyName(string.IsNullOrWhiteSpace(fontFamily) ? "Segoe UI" : fontFamily, weight, SKFontStyleWidth.Normal, slant) ?? SKTypeface.Default;
            using var resolvedFont = new SKFont(typeface, fontSize);

            _canvas.DrawText(text, x, y, resolvedFont, paint);
        }

        public void MeasureText(string text, float fontSize, out float width, out float height, string? fontFamily = null, bool bold = false, bool italic = false) {
            var weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            var slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            using var typeface = SKTypeface.FromFamilyName(string.IsNullOrWhiteSpace(fontFamily) ? "Segoe UI" : fontFamily, weight, SKFontStyleWidth.Normal, slant) ?? SKTypeface.Default;
            using var font = new SKFont(typeface, fontSize);
            font.MeasureText(text, out var bounds);
            width = bounds.Width;
            height = bounds.Height;
        }

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
