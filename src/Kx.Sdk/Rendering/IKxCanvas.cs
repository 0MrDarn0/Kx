// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Rendering;

/// <summary>
/// Represents a renderer-agnostic drawing surface abstraction used by SDK visuals.
/// </summary>
public interface IKxCanvas {
    /// <summary>
    /// Draws a bitmap into the specified destination rectangle.
    /// </summary>
    /// <param name="bitmap">The backend-specific bitmap object.</param>
    /// <param name="left">The left coordinate of the destination rectangle.</param>
    /// <param name="top">The top coordinate of the destination rectangle.</param>
    /// <param name="right">The right coordinate of the destination rectangle.</param>
    /// <param name="bottom">The bottom coordinate of the destination rectangle.</param>
    void DrawBitmap(object bitmap, float left, float top, float right, float bottom);

    /// <summary>
    /// Draws a filled rectangle using renderer-neutral color input.
    /// </summary>
    /// <param name="left">The left coordinate of the rectangle.</param>
    /// <param name="top">The top coordinate of the rectangle.</param>
    /// <param name="right">The right coordinate of the rectangle.</param>
    /// <param name="bottom">The bottom coordinate of the rectangle.</param>
    /// <param name="color">The fill color.</param>
    void DrawRect(float left, float top, float right, float bottom, KxColor color);

    /// <summary>
    /// Draws a stroked rectangle using renderer-neutral color input.
    /// </summary>
    /// <param name="left">The left coordinate of the rectangle.</param>
    /// <param name="top">The top coordinate of the rectangle.</param>
    /// <param name="right">The right coordinate of the rectangle.</param>
    /// <param name="bottom">The bottom coordinate of the rectangle.</param>
    /// <param name="color">The stroke color.</param>
    /// <param name="thickness">The stroke thickness.</param>
    void DrawRectStroke(float left, float top, float right, float bottom, KxColor color, float thickness = 1f);

    /// <summary>
    /// Draws a line segment using renderer-neutral color input.
    /// </summary>
    /// <param name="x0">The x coordinate of the line start.</param>
    /// <param name="y0">The y coordinate of the line start.</param>
    /// <param name="x1">The x coordinate of the line end.</param>
    /// <param name="y1">The y coordinate of the line end.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness.</param>
    void DrawLine(float x0, float y0, float x1, float y1, KxColor color, float thickness = 1f);

    /// <summary>
    /// Draws a filled rounded rectangle using renderer-neutral color input.
    /// </summary>
    /// <param name="left">The left coordinate of the rectangle.</param>
    /// <param name="top">The top coordinate of the rectangle.</param>
    /// <param name="right">The right coordinate of the rectangle.</param>
    /// <param name="bottom">The bottom coordinate of the rectangle.</param>
    /// <param name="radiusX">The horizontal corner radius.</param>
    /// <param name="radiusY">The vertical corner radius.</param>
    /// <param name="color">The fill color.</param>
    void DrawRoundedRect(float left, float top, float right, float bottom, float radiusX, float radiusY, KxColor color);

    /// <summary>
    /// Draws a text string with a simple font configuration.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="x">The x position of the text baseline origin.</param>
    /// <param name="y">The y position of the text baseline origin.</param>
    /// <param name="fontSize">The font size in device-independent units.</param>
    /// <param name="color">The text color.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="bold">Whether bold weight should be used.</param>
    /// <param name="italic">Whether italic slant should be used.</param>
    /// <param name="font">An optional backend-specific font object.</param>
    void DrawText(string text, float x, float y, float fontSize, KxColor color, string? fontFamily = null, bool bold = false, bool italic = false, object? font = null);

    /// <summary>
    /// Measures a text string with the given font configuration.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontSize">The font size in device-independent units.</param>
    /// <param name="width">The measured text width.</param>
    /// <param name="height">The measured text height.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="bold">Whether bold weight should be used.</param>
    /// <param name="italic">Whether italic slant should be used.</param>
    void MeasureText(string text, float fontSize, out float width, out float height, string? fontFamily = null, bool bold = false, bool italic = false);

    /// <summary>
    /// Tries to expose a backend-specific drawing object.
    /// </summary>
    /// <typeparam name="TBackend">The requested backend type.</typeparam>
    /// <param name="backend">The backend instance when available.</param>
    /// <returns><see langword="true"/> when the backend type is available; otherwise <see langword="false"/>.</returns>
    bool TryGetBackend<TBackend>(out TBackend? backend) where TBackend : class;
}
