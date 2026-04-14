// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using System.Drawing.Imaging;
using Kx.Core.Extensions;
using SkiaSharp;

namespace Kx.Utility;

public class FileResourceProvider(string baseDirectory, int strongCacheCapacity = 16) : IResourceProvider {
    private readonly string _baseDirectory = Path.GetFullPath(baseDirectory);
    private readonly ConcurrentDictionary<string, WeakReference<Bitmap>> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _strongCacheLock = new();
    private readonly LinkedList<string> _strongLru = new();
    private readonly Dictionary<string, Bitmap> _strongCache = [];
    private readonly ConcurrentDictionary<string, WeakReference<SKData>> _typefaceCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _strongTypefaceCacheLock = new();
    private readonly LinkedList<string> _strongTypefaceLru = new();
    private readonly Dictionary<string, SKData> _strongTypefaceCache = [];
    private readonly int _strongCacheCapacity = Math.Max(0, strongCacheCapacity);

    private string ResolvePath(string id) {
        if (Path.IsPathRooted(id))
            return id;

        if (id.Contains(':')) {
            var parts = id.Split([':'], 2);
            var ns = parts[0];
            var tail = parts[1]
                .Replace(':', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(_baseDirectory, ns, tail);
        }

        return Path.Combine(_baseDirectory, id.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
    }

    public Stream? OpenStream(string id) {
        var path = ResolvePath(id);
        if (!File.Exists(path))
            return null;
        try {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch {
            return null;
        }
    }

    public async Task<Stream?> OpenStreamAsync(string id, CancellationToken ct = default) {
        var path = ResolvePath(id);
        if (!File.Exists(path))
            return null;
        try {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            return await Task.FromResult(fs);
        }
        catch {
            return null;
        }
    }

    public bool TryGetBitmap(string id, out Bitmap? bitmap) {
        if (_strongCacheCapacity > 0) {
            lock (_strongCacheLock) {
                if (_strongCache.TryGetValue(id, out var bstrong)) {
                    _strongLru.Remove(id);
                    _strongLru.AddFirst(id);
                    bitmap = (Bitmap)bstrong.Clone();
                    return true;
                }
            }
        }

        if (_bitmapCache.TryGetValue(id, out var weak)) {
            if (weak.TryGetTarget(out var cachedBmp)) {
                bitmap = (Bitmap)cachedBmp.Clone();
                return true;
            }
            else {
                _bitmapCache.TryRemove(id, out _);
            }
        }

        var path = ResolvePath(id);
        if (!File.Exists(path)) {
            bitmap = null;
            return false;
        }

        try {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bmp = new Bitmap(fs);
            if (bmp.PixelFormat != PixelFormat.Format32bppPArgb) {
                var conv = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(conv)) {
                    g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                }
                bmp.Dispose();
                bmp = conv;
            }

            _bitmapCache[id] = new WeakReference<Bitmap>(bmp);
            if (_strongCacheCapacity > 0) {
                lock (_strongCacheLock) {
                    if (_strongCache.Count >= _strongCacheCapacity) {
                        var last = _strongLru.Last!.Value;
                        _strongLru.RemoveLast();
                        _strongCache.Remove(last);
                    }
                    _strongCache[id] = (Bitmap)bmp.Clone();
                    _strongLru.AddFirst(id);
                }
            }

            bitmap = (Bitmap)bmp.Clone();
            return true;
        }
        catch {
            bitmap = null;
            return false;
        }
    }

    public SKBitmap? TryGetSkiaBitmap(string id) {
        if (TryGetBitmap(id, out var bmp)) {
            try {
                var sk = bmp?.ToSKBitmap();
                bmp?.Dispose();
                return sk;
            }
            catch {
                bmp?.Dispose();
                return null;
            }
        }
        return null;
    }

    public SKTypeface? TryGetSkiaTypeface(string id) {
        if (TryGetCachedTypefaceData(id, out SKData? cachedTypefaceData))
            return CreateTypeface(cachedTypefaceData);

        string path = ResolvePath(id);
        if (!File.Exists(path))
            return null;

        try {
            SKData typefaceData = SKData.Create(path);
            CacheTypefaceData(id, typefaceData);
            return CreateTypeface(typefaceData);
        }
        catch {
            return null;
        }
    }

    public bool TryGetIntrinsicSize(string id, out Size? size) {
        if (TryGetBitmap(id, out var bmp)) {
            size = bmp?.Size;
            bmp?.Dispose();
            return true;
        }
        size = Size.Empty;
        return false;
    }

    public void Dispose() {
        foreach (var kv in _bitmapCache.ToArray()) {
            if (kv.Value.TryGetTarget(out var bmp)) {
                try { bmp.Dispose(); }
                catch { }
            }
        }
        _bitmapCache.Clear();

        lock (_strongCacheLock) {
            foreach (var kv in _strongCache)
                kv.Value.Dispose();
            _strongCache.Clear();
            _strongLru.Clear();
        }

        HashSet<SKData> disposedTypefaceData = new(ReferenceEqualityComparer.Instance);
        foreach (var kv in _typefaceCache.ToArray()) {
            if (kv.Value.TryGetTarget(out SKData? typefaceData) && disposedTypefaceData.Add(typefaceData)) {
                try { typefaceData.Dispose(); }
                catch { }
            }
        }
        _typefaceCache.Clear();

        lock (_strongTypefaceCacheLock) {
            foreach (var kv in _strongTypefaceCache) {
                if (!disposedTypefaceData.Add(kv.Value))
                    continue;

                kv.Value.Dispose();
            }

            _strongTypefaceCache.Clear();
            _strongTypefaceLru.Clear();
        }
    }

    private bool TryGetCachedTypefaceData(string id, out SKData? typefaceData) {
        if (_strongCacheCapacity > 0) {
            lock (_strongTypefaceCacheLock) {
                if (_strongTypefaceCache.TryGetValue(id, out SKData? strongTypefaceData)) {
                    _strongTypefaceLru.Remove(id);
                    _strongTypefaceLru.AddFirst(id);
                    typefaceData = strongTypefaceData;
                    return true;
                }
            }
        }

        if (_typefaceCache.TryGetValue(id, out WeakReference<SKData>? weakTypefaceData)) {
            if (weakTypefaceData.TryGetTarget(out SKData? cachedTypefaceData)) {
                typefaceData = cachedTypefaceData;
                return true;
            }

            _typefaceCache.TryRemove(id, out _);
        }

        typefaceData = null;
        return false;
    }

    private void CacheTypefaceData(string id, SKData typefaceData) {
        _typefaceCache[id] = new WeakReference<SKData>(typefaceData);

        if (_strongCacheCapacity <= 0)
            return;

        lock (_strongTypefaceCacheLock) {
            if (_strongTypefaceCache.Count >= _strongCacheCapacity) {
                string last = _strongTypefaceLru.Last!.Value;
                _strongTypefaceLru.RemoveLast();
                _strongTypefaceCache.Remove(last);
            }

            _strongTypefaceCache[id] = typefaceData;
            _strongTypefaceLru.AddFirst(id);
        }
    }

    private static SKTypeface? CreateTypeface(SKData? typefaceData) {
        return typefaceData is null
            ? null
            : SKTypeface.FromData(typefaceData);
    }
}
