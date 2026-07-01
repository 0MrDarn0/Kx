// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Sdk.UI.Binding;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.UI.VisualTree;
using Kx.Sdk.Rendering;

namespace Kx.Sdk.UI.Elements;

public abstract class UIElement : Visual, IDockable {
    public UIElement? Parent { get; private set; }
    protected readonly Property<Rectangle> _bounds;
    public override Rectangle Bounds => _bounds.Value;
    public Thickness Margin { get; set; } = new(0);
    public Thickness Padding { get; set; } = new(0);
    public Rectangle? FixedBounds { get; set; }
    public Point VisualOffset { get; set; }
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

    /// <summary>
    /// Assigns the target grid row and column for this element.
    /// </summary>
    /// <param name="row">The zero-based grid row index.</param>
    /// <param name="column">The zero-based grid column index.</param>
    /// <returns>The same element instance for fluent configuration.</returns>
    public UIElement InGrid(int row, int column) {
        GridRow = row;
        GridColumn = column;
        return this;
    }

    /// <summary>
    /// Assigns the target grid row, column, and spans for this element.
    /// </summary>
    /// <param name="row">The zero-based grid row index.</param>
    /// <param name="column">The zero-based grid column index.</param>
    /// <param name="rowSpan">The number of rows to span.</param>
    /// <param name="columnSpan">The number of columns to span.</param>
    /// <returns>The same element instance for fluent configuration.</returns>
    public UIElement InGrid(int row, int column, int rowSpan, int columnSpan) {
        GridRow = row;
        GridColumn = column;
        GridRowSpan = Math.Max(1, rowSpan);
        GridColumnSpan = Math.Max(1, columnSpan);
        return this;
    }

    /// <summary>
    /// Assigns the margin using individual edge values.
    /// </summary>
    /// <param name="left">The left margin.</param>
    /// <param name="top">The top margin.</param>
    /// <param name="right">The right margin.</param>
    /// <param name="bottom">The bottom margin.</param>
    /// <returns>The same element instance for fluent configuration.</returns>
    public UIElement WithMargin(int left, int top, int right, int bottom) {
        Margin = new Thickness(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// Assigns the margin using a uniform value.
    /// </summary>
    /// <param name="all">The uniform margin value.</param>
    /// <returns>The same element instance for fluent configuration.</returns>
    public UIElement WithMargin(int all) {
        Margin = new Thickness(all);
        return this;
    }

    /// <summary>
    /// Assigns the padding using individual edge values.
    /// </summary>
    /// <param name="left">The left padding.</param>
    /// <param name="top">The top padding.</param>
    /// <param name="right">The right padding.</param>
    /// <param name="bottom">The bottom padding.</param>
    /// <returns>The same element instance for fluent configuration.</returns>
    public UIElement WithPadding(int left, int top, int right, int bottom) {
        Padding = new Thickness(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// Assigns the padding using a uniform value.
    /// </summary>
    /// <param name="all">The uniform padding value.</param>
    /// <returns>The same element instance for fluent configuration.</returns>
    public UIElement WithPadding(int all) {
        Padding = new Thickness(all);
        return this;
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
        finalRect = ApplyVisualOffset(finalRect, VisualOffset, dpi);
        LayoutRect = finalRect;
        _bounds.Value = finalRect;
    }

    public override void Draw(IKxCanvas canvas) {
        if (!Visible)
            return;

        OnDraw(canvas);
    }

    protected abstract void OnDraw(IKxCanvas canvas);


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

    private static Rectangle ApplyVisualOffset(Rectangle rect, Point visualOffset, float dpi) {
        return new Rectangle(
            rect.X + (int)(visualOffset.X * dpi),
            rect.Y + (int)(visualOffset.Y * dpi),
            rect.Width,
            rect.Height);
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
}
