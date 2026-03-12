// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Abstractions.UI.Binding;
using Kx.Abstractions.UI.Layout;
using Kx.Abstractions.UI.VisualTree;

using SkiaSharp;

namespace Kx.Abstractions.UI.Elements;

public abstract class UIElement : Visual, IDockable {
    public UIElement? Parent { get; private set; }
    protected readonly Property<Rectangle> _bounds;
    public override Rectangle Bounds => _bounds.Value;
    public Thickness Margin { get; set; } = new(0);
    public Thickness Padding { get; set; } = new(0);
    public Rectangle? FixedBounds { get; set; }
    public Rectangle LayoutRect { get; protected set; }
    public Size DesiredSize { get; protected set; }
    public Rectangle ContentRect => ApplyPadding(LayoutRect, Padding, DpiScale);
    public Dock Dock { get; set; } = Dock.Fill;
    public int GridRow { get; set; }
    public int GridColumn { get; set; }
    public int GridRowSpan { get; set; } = 1;
    public int GridColumnSpan { get; set; } = 1;

    protected UIElement(IVisualContext ctx, string id) : base(ctx, id) {
        _bounds = new Property<Rectangle>(ctx.UiThread, Rectangle.Empty, Invalidate);
    }

    public void SetParent(UIElement? parent) {
        Parent = parent;
    }

    public override void Measure(float dpi) {
        if (FixedBounds is Rectangle fixedBounds) {
            DesiredSize = AddMargin(AddPadding(fixedBounds.Size, Padding, dpi), Margin, dpi);
            return;
        }

        DesiredSize = AddMargin(AddPadding(Size.Empty, Padding, dpi), Margin, dpi);
    }

    public override void Arrange(Rectangle finalRect, float dpi) {
        if (FixedBounds is Rectangle fixedBounds)
            finalRect = ResolveFixedBounds(finalRect, fixedBounds, dpi);

        finalRect = ApplyMargin(finalRect, Margin, dpi);
        LayoutRect = finalRect;
        _bounds.Value = finalRect;
    }

    public override void Draw(SKCanvas canvas) {
        if (!Visible)
            return;

        OnDraw(canvas);

        if (DebugOverlay.Enabled)
            DrawDebugOverlay(canvas);
    }

    protected virtual void DrawDebugOverlay(SKCanvas canvas) {
        if (!DebugOverlay.Enabled)
            return;

        var fontSize = DebugOverlay.FontSize * DpiScale;
        using var font = new SKFont(SKTypeface.Default, fontSize);
        using var textPaint = new SKPaint { IsAntialias = true, Color = DebugOverlay.TextColor };
        using var bgPaint = new SKPaint { IsAntialias = true, Color = DebugOverlay.TextBgColor, Style = SKPaintStyle.Fill };
        using var boundsPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true, Color = DebugOverlay.BoundsColor };
        using var layoutPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true, Color = DebugOverlay.LayoutColor };

        var clip = canvas.LocalClipBounds;
        var canvasRect = new SKRect(0, 0, clip.Width, clip.Height);

        if (DebugOverlay.ShowBounds)
            canvas.DrawRect(ToSkRect(Bounds), boundsPaint);

        if (DebugOverlay.ShowLayoutRect)
            canvas.DrawRect(ToSkRect(LayoutRect), layoutPaint);

        var metaItems = new List<string>();
        if (DebugOverlay.ShowMeta)
            metaItems.Add($"{Id}  L:{Layer} Z:{ZIndex}  {LayoutRect.Width}x{LayoutRect.Height}");

        var parentItems = new List<string>();
        if (DebugOverlay.ShowParentChain) {
            var current = this;
            int depth = 0;
            while (current != null && depth < DebugOverlay.MaxParentItems) {
                parentItems.Add($"{new string(' ', depth * 2)}{current.Id} (L:{current.Layer} Z:{current.ZIndex})");
                current = current.Parent;
                depth++;
            }
        }

        if (metaItems.Count == 0 && parentItems.Count == 0)
            return;

        float startX = LayoutRect.Left;
        float baseY = LayoutRect.Top + LayoutRect.Height + (DebugOverlay.ItemSpacing * DpiScale);
        float itemPadding = DebugOverlay.ItemPadding * DpiScale;
        float itemSpacing = DebugOverlay.ItemSpacing * DpiScale;

        static void MeasureText(SKFont font, string text, out float width, out float height) {
            font.MeasureText(text, out SKRect tb);
            width = tb.Width;
            height = font.Size;
        }

        float occupiedHeight = 0f;
        if (metaItems.Count > 0) {
            float y = baseY;
            foreach (var text in metaItems) {
                var maxWidth = Math.Max(20f, canvasRect.Width - startX - itemPadding * 2f);
                var drawText = text;
                MeasureText(font, drawText, out float tw, out float th);
                if (tw > maxWidth) {
                    drawText = TruncateTextToWidth(drawText, font, maxWidth);
                    MeasureText(font, drawText, out tw, out th);
                }

                float itemW = tw + itemPadding * 2f;
                float itemH = th + itemPadding * 2f;
                var candidate = new SKRect(startX, y, startX + itemW, y + itemH);

                if (candidate.Bottom > canvasRect.Bottom)
                    break;

                canvas.DrawRect(candidate, bgPaint);
                var textX = candidate.Left + itemPadding;
                var textY = candidate.Top + itemPadding + th;
                canvas.DrawText(drawText, textX, textY, font, textPaint);

                y += itemH + itemSpacing;
                occupiedHeight = y - baseY;
            }
        }

        if (parentItems.Count > 0) {
            float parentStartY = baseY + Math.Max(occupiedHeight, 0f) + itemSpacing;
            float y = parentStartY;

            foreach (var text in parentItems) {
                var maxWidth = Math.Max(20f, canvasRect.Width - startX - itemPadding * 2f);
                var drawText = text;
                MeasureText(font, drawText, out float tw, out float th);
                if (tw > maxWidth) {
                    drawText = TruncateTextToWidth(drawText, font, maxWidth);
                    MeasureText(font, drawText, out tw, out th);
                }

                float itemW = tw + itemPadding * 2f;
                float itemH = th + itemPadding * 2f;
                var candidate = new SKRect(startX, y, startX + itemW, y + itemH);

                if (candidate.Bottom > canvasRect.Bottom)
                    break;

                canvas.DrawRect(candidate, bgPaint);
                var textX = candidate.Left + itemPadding;
                var textY = candidate.Top + itemPadding + th;
                canvas.DrawText(drawText, textX, textY, font, textPaint);

                y += itemH + itemSpacing;
            }
        }
    }

    protected abstract void OnDraw(SKCanvas canvas);

    private static Size AddMargin(Size size, Thickness margin, float dpi) {
        return new Size(
            size.Width + (int)(margin.Horizontal * dpi),
            size.Height + (int)(margin.Vertical * dpi));
    }

    private static Size AddPadding(Size size, Thickness padding, float dpi) {
        return new Size(
            size.Width + (int)(padding.Horizontal * dpi),
            size.Height + (int)(padding.Vertical * dpi));
    }

    private static Rectangle ApplyMargin(Rectangle rect, Thickness margin, float dpi) {
        return new Rectangle(
            rect.X + (int)(margin.Left * dpi),
            rect.Y + (int)(margin.Top * dpi),
            rect.Width - (int)(margin.Horizontal * dpi),
            rect.Height - (int)(margin.Vertical * dpi));
    }

    private static Rectangle ApplyPadding(Rectangle rect, Thickness padding, float dpi) {
        return new Rectangle(
            rect.X + (int)(padding.Left * dpi),
            rect.Y + (int)(padding.Top * dpi),
            rect.Width - (int)(padding.Horizontal * dpi),
            rect.Height - (int)(padding.Vertical * dpi));
    }

    private static Rectangle ResolveFixedBounds(Rectangle layoutRect, Rectangle fixedBounds, float dpi) {
        int width = Math.Max(0, (int)(fixedBounds.Width * dpi));
        int height = Math.Max(0, (int)(fixedBounds.Height * dpi));

        int x = fixedBounds.X >= 0
            ? layoutRect.X + (int)(fixedBounds.X * dpi)
            : layoutRect.Right + (int)(fixedBounds.X * dpi) - width;

        int y = fixedBounds.Y >= 0
            ? layoutRect.Y + (int)(fixedBounds.Y * dpi)
            : layoutRect.Bottom + (int)(fixedBounds.Y * dpi) - height;

        return new Rectangle(x, y, width, height);
    }

    private static SKRect ToSkRect(Rectangle rect) {
        return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    private static string TruncateTextToWidth(string text, SKFont font, float maxWidth) {
        if (string.IsNullOrEmpty(text))
            return text;

        font.MeasureText(text, out SKRect tb);
        if (tb.Width <= maxWidth)
            return text;

        const string ellipsis = "…";
        int left = 0;
        int right = text.Length;
        string candidate;

        while (left <= right) {
            int mid = (left + right) / 2;
            candidate = string.Concat(text.AsSpan(0, Math.Max(0, mid)), ellipsis);
            font.MeasureText(candidate, out SKRect truncatedBounds);
            if (truncatedBounds.Width > maxWidth)
                right = mid - 1;
            else
                left = mid + 1;
        }

        int length = Math.Max(0, left - 1);
        candidate = string.Concat(text.AsSpan(0, Math.Min(length, text.Length)), ellipsis);

        while (true) {
            font.MeasureText(candidate, out SKRect fittedBounds);
            if (fittedBounds.Width <= maxWidth || candidate.Length <= 1)
                break;

            candidate = string.Concat(candidate.AsSpan(0, candidate.Length - 2), ellipsis);
        }

        return candidate;
    }
}
