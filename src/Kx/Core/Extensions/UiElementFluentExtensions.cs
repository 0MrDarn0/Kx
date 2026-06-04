// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Layout;

using SkiaSharp;

namespace Kx.Core.Extensions;

/// <summary>
/// Provides fluent helpers for commonly repeated UI element layout configuration.
/// </summary>
public static class UiElementFluentExtensions {
    /// <summary>
    /// Assigns the target grid row and column.
    /// </summary>
    /// <typeparam name="TElement">The concrete UI element type.</typeparam>
    /// <param name="element">The target UI element.</param>
    /// <param name="row">The zero-based grid row index.</param>
    /// <param name="column">The zero-based grid column index.</param>
    /// <returns>The same UI element instance to support fluent chaining.</returns>
    public static TElement InGrid<TElement>(this TElement element, int row, int column) where TElement : UIElement {
        ArgumentNullException.ThrowIfNull(element);
        element.GridRow = row;
        element.GridColumn = column;
        return element;
    }

    /// <summary>
    /// Assigns the target grid row, column, and spans.
    /// </summary>
    /// <typeparam name="TElement">The concrete UI element type.</typeparam>
    /// <param name="element">The target UI element.</param>
    /// <param name="row">The zero-based grid row index.</param>
    /// <param name="column">The zero-based grid column index.</param>
    /// <param name="rowSpan">The number of rows to span.</param>
    /// <param name="columnSpan">The number of columns to span.</param>
    /// <returns>The same UI element instance to support fluent chaining.</returns>
    public static TElement InGrid<TElement>(this TElement element, int row, int column, int rowSpan, int columnSpan) where TElement : UIElement {
        ArgumentNullException.ThrowIfNull(element);
        element.GridRow = row;
        element.GridColumn = column;
        element.GridRowSpan = Math.Max(1, rowSpan);
        element.GridColumnSpan = Math.Max(1, columnSpan);
        return element;
    }

    /// <summary>
    /// Assigns margin using individual edge values.
    /// </summary>
    /// <typeparam name="TElement">The concrete UI element type.</typeparam>
    /// <param name="element">The target UI element.</param>
    /// <param name="left">The left margin.</param>
    /// <param name="top">The top margin.</param>
    /// <param name="right">The right margin.</param>
    /// <param name="bottom">The bottom margin.</param>
    /// <returns>The same UI element instance to support fluent chaining.</returns>
    public static TElement WithMargin<TElement>(this TElement element, int left, int top, int right, int bottom) where TElement : UIElement {
        ArgumentNullException.ThrowIfNull(element);
        element.Margin = new Thickness(left, top, right, bottom);
        return element;
    }

    /// <summary>
    /// Assigns margin using a uniform value.
    /// </summary>
    /// <typeparam name="TElement">The concrete UI element type.</typeparam>
    /// <param name="element">The target UI element.</param>
    /// <param name="all">The uniform margin value.</param>
    /// <returns>The same UI element instance to support fluent chaining.</returns>
    public static TElement WithMargin<TElement>(this TElement element, int all) where TElement : UIElement {
        ArgumentNullException.ThrowIfNull(element);
        element.Margin = new Thickness(all);
        return element;
    }

    /// <summary>
    /// Assigns padding using individual edge values.
    /// </summary>
    /// <typeparam name="TElement">The concrete UI element type.</typeparam>
    /// <param name="element">The target UI element.</param>
    /// <param name="left">The left padding.</param>
    /// <param name="top">The top padding.</param>
    /// <param name="right">The right padding.</param>
    /// <param name="bottom">The bottom padding.</param>
    /// <returns>The same UI element instance to support fluent chaining.</returns>
    public static TElement WithPadding<TElement>(this TElement element, int left, int top, int right, int bottom) where TElement : UIElement {
        ArgumentNullException.ThrowIfNull(element);
        element.Padding = new Thickness(left, top, right, bottom);
        return element;
    }

    /// <summary>
    /// Assigns padding using a uniform value.
    /// </summary>
    /// <typeparam name="TElement">The concrete UI element type.</typeparam>
    /// <param name="element">The target UI element.</param>
    /// <param name="all">The uniform padding value.</param>
    /// <returns>The same UI element instance to support fluent chaining.</returns>
    public static TElement WithPadding<TElement>(this TElement element, int all) where TElement : UIElement {
        ArgumentNullException.ThrowIfNull(element);
        element.Padding = new Thickness(all);
        return element;
    }

    /// <summary>
    /// Assigns the foreground color of a label.
    /// </summary>
    /// <param name="label">The target label.</param>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same label instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.Label WithForeground(this Kx.UI.Elements.Label label, SKColor color) {
        ArgumentNullException.ThrowIfNull(label);
        label.Color.Value = color;
        return label;
    }

    /// <summary>
    /// Assigns the foreground color of a button.
    /// </summary>
    /// <param name="button">The target button.</param>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same button instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.Button WithForeground(this Kx.UI.Elements.Button button, SKColor color) {
        ArgumentNullException.ThrowIfNull(button);
        button.ForegroundColor = color;
        return button;
    }

    /// <summary>
    /// Assigns the foreground color of a text box.
    /// </summary>
    /// <param name="textBox">The target text box.</param>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same text box instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.TextBox WithForeground(this Kx.UI.Elements.TextBox textBox, SKColor color) {
        ArgumentNullException.ThrowIfNull(textBox);
        textBox.ForegroundColor = color;
        return textBox;
    }

    /// <summary>
    /// Assigns the foreground color of a list box.
    /// </summary>
    /// <param name="listBox">The target list box.</param>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same list box instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.ListBox WithForeground(this Kx.UI.Elements.ListBox listBox, SKColor color) {
        ArgumentNullException.ThrowIfNull(listBox);
        listBox.ForegroundColor = color;
        return listBox;
    }

    /// <summary>
    /// Assigns the background color of a button.
    /// </summary>
    /// <param name="button">The target button.</param>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same button instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.Button WithBackground(this Kx.UI.Elements.Button button, SKColor color) {
        ArgumentNullException.ThrowIfNull(button);
        button.BackgroundColor = color;
        return button;
    }

    /// <summary>
    /// Assigns the border color and thickness of a button.
    /// </summary>
    /// <param name="button">The target button.</param>
    /// <param name="color">The border color to apply.</param>
    /// <param name="thickness">The border thickness to apply.</param>
    /// <returns>The same button instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.Button WithBorder(this Kx.UI.Elements.Button button, SKColor color, float thickness) {
        ArgumentNullException.ThrowIfNull(button);
        button.BorderColor = color;
        return button;
    }

    /// <summary>
    /// Assigns the background color of a text box.
    /// </summary>
    /// <param name="textBox">The target text box.</param>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same text box instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.TextBox WithBackground(this Kx.UI.Elements.TextBox textBox, SKColor color) {
        ArgumentNullException.ThrowIfNull(textBox);
        textBox.BackgroundColor = color;
        return textBox;
    }

    /// <summary>
    /// Assigns the border color and thickness of a text box.
    /// </summary>
    /// <param name="textBox">The target text box.</param>
    /// <param name="color">The border color to apply.</param>
    /// <param name="thickness">The border thickness to apply.</param>
    /// <returns>The same text box instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.TextBox WithBorder(this Kx.UI.Elements.TextBox textBox, SKColor color, float thickness) {
        ArgumentNullException.ThrowIfNull(textBox);
        textBox.BorderColor = color;
        textBox.BorderThickness = thickness;
        return textBox;
    }

    /// <summary>
    /// Assigns the background color of a list box.
    /// </summary>
    /// <param name="listBox">The target list box.</param>
    /// <param name="color">The color to apply.</param>
    /// <returns>The same list box instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.ListBox WithBackground(this Kx.UI.Elements.ListBox listBox, SKColor color) {
        ArgumentNullException.ThrowIfNull(listBox);
        listBox.BackgroundColor = color;
        return listBox;
    }

    /// <summary>
    /// Assigns the border color and thickness of a list box.
    /// </summary>
    /// <param name="listBox">The target list box.</param>
    /// <param name="color">The border color to apply.</param>
    /// <param name="thickness">The border thickness to apply.</param>
    /// <returns>The same list box instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.ListBox WithBorder(this Kx.UI.Elements.ListBox listBox, SKColor color, float thickness) {
        ArgumentNullException.ThrowIfNull(listBox);
        listBox.BorderColor = color;
        listBox.BorderThickness = thickness;
        return listBox;
    }

    /// <summary>
    /// Assigns button background colors for all interaction states.
    /// </summary>
    /// <param name="button">The target button.</param>
    /// <param name="normal">The default background color.</param>
    /// <param name="hover">The hover background color.</param>
    /// <param name="pressed">The pressed background color.</param>
    /// <param name="disabled">The disabled background color.</param>
    /// <returns>The same button instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.Button WithButtonStates(this Kx.UI.Elements.Button button, SKColor normal, SKColor hover, SKColor pressed, SKColor disabled) {
        ArgumentNullException.ThrowIfNull(button);
        button.BackgroundColor = normal;
        button.HoverBackgroundColor = hover;
        button.PressedBackgroundColor = pressed;
        button.DisabledBackgroundColor = disabled;
        return button;
    }

    /// <summary>
    /// Attaches a click handler to a button.
    /// </summary>
    /// <param name="button">The target button.</param>
    /// <param name="onClick">The callback invoked for click events.</param>
    /// <returns>The same button instance to support fluent chaining.</returns>
    public static Kx.UI.Elements.Button OnClick(this Kx.UI.Elements.Button button, Action onClick) {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(onClick);
        button.Click += onClick;
        return button;
    }
}
