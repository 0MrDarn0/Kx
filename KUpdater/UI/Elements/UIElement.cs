// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Core.Extensions;
using KUpdater.UI.Binding;
using KUpdater.UI.Layout;
using KUpdater.UI.VisualTree;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace KUpdater.UI.Elements;

public abstract class UIElement : Visual, IDockable {
    public UIElement? Parent { get; internal set; }
    public bool UseContentArea { get; set; } = true;
    protected readonly Property<Rectangle> _bounds;
    public Rectangle Bounds => _bounds.Value;
    public Thickness Margin { get; set; } = new Thickness(0);
    public Thickness Padding { get; set; } = new Thickness(0);
    public Rectangle LayoutRect { get; protected set; }
    public Size DesiredSize { get; protected set; }
    public Rectangle ContentRect => LayoutRect.ApplyPadding(Padding, DpiScale);
    public Dock Dock { get; set; } = Dock.Fill;
    public int GridRow { get; set; }
    public int GridColumn { get; set; }
    public int GridRowSpan { get; set; } = 1;
    public int GridColumnSpan { get; set; } = 1;

    private readonly WindowContext _ctx;

    protected UIElement(WindowContext ctx, string id) : base(ctx, id) {
        _ctx = ctx;
        _bounds = new Property<Rectangle>(ctx.UiThread, Rectangle.Empty, () => Invalidate());
    }

    public override void Measure(float dpi) {
        DesiredSize = new Size(0, 0)
            .AddPadding(Padding, dpi)
            .AddMargin(Margin, dpi);
    }

    public override void Arrange(Rectangle finalRect, float dpi) {
        finalRect = finalRect.ApplyMargin(Margin, dpi);
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

        // Setup paints / font
        var fontSize = DebugOverlay.FontSize * DpiScale;
        using var font = new SKFont(SKTypeface.Default, fontSize);
        using var textPaint = new SKPaint { IsAntialias = true, Color = DebugOverlay.TextColor };
        using var bgPaint = new SKPaint { IsAntialias = true, Color = DebugOverlay.TextBgColor, Style = SKPaintStyle.Fill };
        using var boundsPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true, Color = DebugOverlay.BoundsColor };
        using var layoutPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true, Color = DebugOverlay.LayoutColor };

        // Canvas bounds for clamping
        var clip = canvas.LocalClipBounds;
        var canvasRect = new SKRect(0, 0, clip.Width, clip.Height);

        // Draw rects first
        if (DebugOverlay.ShowBounds)
            canvas.DrawRect(Bounds.ToSKRect(), boundsPaint);

        if (DebugOverlay.ShowLayoutRect)
            canvas.DrawRect(LayoutRect.ToSKRect(), layoutPaint);

        // Prepare meta and parent items separately so we can stack parent chain below meta
        var metaItems = new List<string>();
        if (DebugOverlay.ShowMeta)
            metaItems.Add($"{Id}  L:{Layer} Z:{ZIndex}  {LayoutRect.Width}x{LayoutRect.Height}");

        var parentItems = new List<string>();
        if (DebugOverlay.ShowParentChain) {
            var current = this as UIElement;
            int depth = 0;
            while (current != null && depth < DebugOverlay.MaxParentItems) {
                parentItems.Add($"{new string(' ', depth * 2)}{current.Id} (L:{current.Layer} Z:{current.ZIndex})");
                current = current.Parent;
                depth++;
            }
        }

        if (metaItems.Count == 0 && parentItems.Count == 0)
            return;

        // Layout parameters
        float startX = LayoutRect.Left;
        float baseY = LayoutRect.Top + LayoutRect.Height + (DebugOverlay.ItemSpacing * DpiScale);
        float itemPadding = DebugOverlay.ItemPadding * DpiScale;
        float itemSpacing = DebugOverlay.ItemSpacing * DpiScale;

        // Helper: compute item size for a text
        static void MeasureText(SKFont font, string text, out float width, out float height) {
            font.MeasureText(text, out SKRect tb);
            width = tb.Width;
            height = font.Size;
        }

        // Draw meta items first and remember occupied height
        float occupiedHeight = 0f;
        if (metaItems.Count > 0) {
            float y = baseY;
            foreach (var text in metaItems) {
                // clamp width to canvas
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
                var textX = candidate.Left + itemPadding - /*tb.Left*/ 0f;
                var textY = candidate.Top + itemPadding + th; // baseline approx
                canvas.DrawText(drawText, textX, textY, font, textPaint);

                y += itemH + itemSpacing;
                occupiedHeight = y - baseY;
            }
        }

        // Draw parent chain below meta (with extra spacing)
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
                var textX = candidate.Left + itemPadding - /*tb.Left*/ 0f;
                var textY = candidate.Top + itemPadding + th;
                canvas.DrawText(drawText, textX, textY, font, textPaint);

                y += itemH + itemSpacing;
            }
        }
    }

    // Hilfsfunktion: Text kürzen mit Ellipse so, dass er in maxWidth passt
    private static string TruncateTextToWidth(string text, SKFont font, float maxWidth) {
        if (string.IsNullOrEmpty(text))
            return text;
        font.MeasureText(text, out SKRect tb);
        if (tb.Width <= maxWidth)
            return text;

        const string ell = "…";
        int left = 0;
        int right = text.Length;
        string candidate;

        // Binäre Suche auf Länge
        while (left <= right) {
            int mid = (left + right) / 2;
            candidate = string.Concat(text.AsSpan(0, Math.Max(0, mid)), ell);
            font.MeasureText(candidate, out SKRect tb2);
            if (tb2.Width > maxWidth)
                right = mid - 1;
            else
                left = mid + 1;
        }

        int len = Math.Max(0, left - 1);
        candidate = string.Concat(text.AsSpan(0, Math.Min(len, text.Length)), ell);

        // Feinjustierung: entferne Zeichen bis es passt
        while (true) {
            font.MeasureText(candidate, out SKRect tb3);
            if (tb3.Width <= maxWidth)
                break;
            if (candidate.Length <= 1)
                break;
            candidate = string.Concat(candidate.AsSpan(0, candidate.Length - 2), ell);
        }

        return candidate;
    }
    protected abstract void OnDraw(SKCanvas canvas);
}
