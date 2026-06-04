// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Rendering;

/// <summary>
/// Represents a renderer-agnostic drawing surface abstraction used by SDK visuals.
/// </summary>
public interface IKxCanvas {
    /// <summary>
    /// Tries to expose a backend-specific drawing object.
    /// </summary>
    /// <typeparam name="TBackend">The requested backend type.</typeparam>
    /// <param name="backend">The backend instance when available.</param>
    /// <returns><see langword="true"/> when the backend type is available; otherwise <see langword="false"/>.</returns>
    bool TryGetBackend<TBackend>(out TBackend? backend) where TBackend : class;
}
