// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Layout;

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
}
