// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Rendering;

/// <summary>
/// Provides access to backend-specific drawing objects behind <see cref="IKxCanvas"/>.
/// </summary>
public static class KxCanvasExtensions {
    /// <summary>
    /// Returns the backend-specific drawing object for the requested type.
    /// </summary>
    /// <typeparam name="T">The backend object type.</typeparam>
    /// <param name="canvas">The abstract canvas instance.</param>
    /// <returns>The backend object when available; otherwise <see langword="null"/>.</returns>
    public static T? As<T>(this IKxCanvas canvas) where T : class {
        ArgumentNullException.ThrowIfNull(canvas);

        return canvas.TryGetBackend<T>(out var backend)
            ? backend
            : null;
    }
}
