// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Rendering;

using SkiaSharp;

namespace Kx.UI.Rendering;

/// <summary>
/// Bridges the SDK drawing abstraction to the active SkiaSharp canvas instance.
/// </summary>
internal sealed class SkiaCanvas : IKxCanvas {
    private readonly SKCanvas _canvas;

    /// <summary>
    /// Initializes a wrapper for a concrete SkiaSharp canvas.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas used by the renderer.</param>
    public SkiaCanvas(SKCanvas canvas) {
        ArgumentNullException.ThrowIfNull(canvas);
        _canvas = canvas;
    }

    /// <summary>
    /// Tries to expose the wrapped backend object for interop scenarios.
    /// </summary>
    /// <typeparam name="TBackend">The requested backend type.</typeparam>
    /// <param name="backend">The backend object when the type matches.</param>
    /// <returns><see langword="true"/> when the backend type matches; otherwise <see langword="false"/>.</returns>
    public bool TryGetBackend<TBackend>(out TBackend? backend) where TBackend : class {
        if (_canvas is TBackend typedCanvas) {
            backend = typedCanvas;
            return true;
        }

        backend = null;
        return false;
    }
}
