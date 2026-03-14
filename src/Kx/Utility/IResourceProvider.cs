// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace Kx.Utility;

public interface IResourceProvider : IDisposable {
    /// Öffnet asynchron einen Stream für die resource id oder null wenn nicht vorhanden.
    public Task<Stream?> OpenStreamAsync(string id, CancellationToken ct = default);

    /// Öffnet synchron (falls unterstützt) einen Stream oder null.
    public Stream? OpenStream(string id);

    /// Versucht synchron ein System.Drawing.Bitmap zurückzugeben; schnell, ohne Exception.
    public bool TryGetBitmap(string id, out Bitmap? bitmap);

    /// Versucht synchron ein SKBitmap zurückzugeben; null wenn nicht vorhanden.
    public SKBitmap? TryGetSkiaBitmap(string id);

    /// Optional: gibt die intrinsische Pixelgröße zurück (wenn bekannt).
    public bool TryGetIntrinsicSize(string id, out Size? size);
}
