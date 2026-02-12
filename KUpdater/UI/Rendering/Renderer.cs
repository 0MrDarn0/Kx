// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KUpdater.Core;
using KUpdater.Extensions;
using KUpdater.Interop;
using KUpdater.UI.Interface;
using SkiaSharp;

namespace KUpdater.UI.Rendering;

public unsafe class Renderer : IRenderer, IDisposable {
    private readonly WindowContext _ctx;

    // Steuerflags
    private int _needsRender;
    private int _invokePending;
    private int _isRenderingFlag;

    // Skia / Backbuffers
    private SKBitmap? _renderBuffer;
    private SKSurface? _renderSurface;

    // Background worker buffers
    private readonly CancellationTokenSource _renderCts = new();
    private Task? _renderWorker;
    private readonly object _swapLock = new();
    private SKBitmap? _bgRenderBitmap;    // vom Worker beschrieben
    private SKBitmap? _uiPresentBitmap;   // vom Worker getauscht, bereit für Present
    private volatile bool _workerRunning;

    // DIBSection mit FileMapping (UI-Thread erstellt, Worker schreibt direkt hinein)
    private IntPtr _hSection = IntPtr.Zero; // FileMapping handle
    private IntPtr _hDib = IntPtr.Zero;     // HBITMAP (DIBSection)
    private IntPtr _dibPixels = IntPtr.Zero; // Pointer auf Bitmapbits
    private int _dibWidth;
    private int _dibHeight;
    private readonly object _dibLock = new(); // schützt _dibPixels/_hDib/_hSection während Schreiben/Present

    private readonly SKPaint _fillPaint = new() { IsAntialias = true };

    // Persistent zero-row buffer für Fallbacks
    private byte[]? _zeroRowBuffer;
    private int _zeroRowBufferSize;

    private readonly List<SKRect> _missingRects = new();
    public long LastRenderDurationMs { get; private set; }
    public int LastPresentError { get; private set; }

    // Debug / Overlay
    private bool _showDebugRasterOverlay = false;
    public void ToggleDebugOverlay() => _showDebugRasterOverlay = !_showDebugRasterOverlay;
    public void SetDebugOverlay(bool enabled) => _showDebugRasterOverlay = enabled;

    // Performance Overlay
    private readonly object _perfLock = new();
    private readonly Queue<long> _frameTimestamps = new();
    private const int FrameHistory = 60;
    private bool _showPerfOverlay = false;
    public void TogglePerfOverlay() => _showPerfOverlay = !_showPerfOverlay;
    public void SetPerfOverlay(bool enabled) => _showPerfOverlay = enabled;

    private bool _disposed;

    public Renderer(WindowContext ctx) {
        _ctx = ctx;
        StartRenderWorker();
    }

    public void RequestRender() => Interlocked.Exchange(ref _needsRender, 1);

    public void Resize(int width, int height) => EnsureBuffers(width, height);

    // Ensure Skia buffers
    public void EnsureBuffers(int width, int height) {
        if (width <= 0 || height <= 0)
            return;

        if (_renderBuffer == null || _renderBuffer.Width != width || _renderBuffer.Height != height) {
            try { _renderSurface?.Dispose(); }
            catch { }
            try { _renderBuffer?.Dispose(); }
            catch { }
            _renderBuffer = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _renderSurface = SKSurface.Create(_renderBuffer.Info, _renderBuffer.GetPixels(), _renderBuffer.RowBytes);
        }
    }

    // Worker lifecycle
    private void StartRenderWorker() {
        if (_workerRunning)
            return;
        _workerRunning = true;
        _renderWorker = Task.Run(() => RenderWorkerLoop(_renderCts.Token));
    }

    private void StopRenderWorker() {
        _renderCts.Cancel();
        try { _renderWorker?.Wait(500); }
        catch { }
        _workerRunning = false;
    }

    private void GetDeviceSize(out int deviceWidth, out int deviceHeight) {
        float scale = Math.Max(1f, _ctx.Target.DeviceDpi / 96f);
        deviceWidth = (int)Math.Ceiling(_ctx.Target.Width * scale);
        deviceHeight = (int)Math.Ceiling(_ctx.Target.Height * scale);
        if (deviceWidth <= 0)
            deviceWidth = 1;
        if (deviceHeight <= 0)
            deviceHeight = 1;
    }

    private void RecordFrameTimestamp() {
        long now = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
        lock (_perfLock) {
            _frameTimestamps.Enqueue(now);
            while (_frameTimestamps.Count > FrameHistory)
                _frameTimestamps.Dequeue();
        }
    }

    private void EnsureZeroRowBuffer(int size) {
        if (_zeroRowBuffer == null || _zeroRowBufferSize < size) {
            _zeroRowBuffer = new byte[size];
            _zeroRowBufferSize = size;
        }
    }

    // mapped DIBSection (CreateFileMapping + CreateDIBSection mit hSection)
    // Muss auf UI-Thread laufen
    private bool EnsureMappedDib(int width, int height) {
        if (width <= 0 || height <= 0)
            return false;

        lock (_dibLock) {
            // Wenn bereits passend vorhanden, nichts tun
            if (_hDib != IntPtr.Zero && _dibWidth == width && _dibHeight == height && _dibPixels != IntPtr.Zero)
                return true;

            // Alte Ressourcen freigeben
            try {
                if (_hDib != IntPtr.Zero) {
                    _ = NativeMethods.DeleteObject(_hDib);
                    _hDib = IntPtr.Zero;
                }
            }
            catch { }

            try {
                if (_hSection != IntPtr.Zero) {
                    _ = NativeMethods.CloseHandle(_hSection);
                    _hSection = IntPtr.Zero;
                }
            }
            catch { }

            // Größe in Bytes
            long bytes = (long)width * height * 4;
            if (bytes <= 0 || bytes > (long)int.MaxValue * 4L) {
                Debug.WriteLine("Requested DIB too large");
                return false;
            }

            uint sizeLow = (uint)(bytes & 0xFFFFFFFF);
            uint sizeHigh = (uint)((bytes >> 32) & 0xFFFFFFFF);

            // CreateFileMapping mit INVALID_HANDLE_VALUE -> page-backed (kein File)
            IntPtr hSection = NativeMethods.CreateFileMapping(NativeMethods.INVALID_HANDLE_VALUE, IntPtr.Zero, NativeMethods.PAGE_READWRITE, sizeHigh, sizeLow, null);
            if (hSection == IntPtr.Zero) {
                var err = Marshal.GetLastWin32Error();
                Debug.WriteLine($"CreateFileMapping failed: {err}");
                return false;
            }

            // Erzeuge BITMAPINFO
            var bmi = new NativeMethods.BITMAPINFO {
                bmiHeader = new NativeMethods.BITMAPINFOHEADER {
                    biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = NativeMethods.BI_RGB,
                    biSizeImage = (uint)(width * height * 4)
                },
                bmiColors = new uint[3]
            };

            IntPtr dibPixels;
            IntPtr hDib = NativeMethods.CreateDIBSection(IntPtr.Zero, ref bmi, NativeMethods.DIB_RGB_COLORS, out dibPixels, hSection, 0);
            if (hDib == IntPtr.Zero || dibPixels == IntPtr.Zero) {
                var err = Marshal.GetLastWin32Error();
                Debug.WriteLine($"CreateDIBSection with mapping failed: {err}");
                // Aufräumen
                try { _ = NativeMethods.CloseHandle(hSection); }
                catch { }
                return false;
            }

            // Speichere Handles
            _hSection = hSection;
            _hDib = hDib;
            _dibPixels = dibPixels;
            _dibWidth = width;
            _dibHeight = height;

            return true;
        }
    }

    private void ReleaseMappedDib() {
        lock (_dibLock) {
            try {
                if (_hDib != IntPtr.Zero) {
                    _ = NativeMethods.DeleteObject(_hDib);
                    _hDib = IntPtr.Zero;
                }
            }
            catch { }

            try {
                if (_hSection != IntPtr.Zero) {
                    _ = NativeMethods.CloseHandle(_hSection);
                    _hSection = IntPtr.Zero;
                }
            }
            catch { }

            _dibPixels = IntPtr.Zero;
            _dibWidth = 0;
            _dibHeight = 0;
        }
    }

    // Zeichnet im Hintergrund und schreibt direkt in DIB (wenn möglich)
    private void RenderWorkerLoop(CancellationToken ct) {
        try {
            while (!ct.IsCancellationRequested) {
                // nur weiter wenn ein Render angefordert wurde
                if (Interlocked.Exchange(ref _needsRender, 0) == 0) {
                    Thread.Sleep(8);
                    continue;
                }

                // Größe vom UI-Thread holen
                int width = 0, height = 0;
                try {
                    _ctx.UiThread.Invoke(new Action(() => GetDeviceSize(out width, out height)));
                }
                catch {
                    continue;
                }

                if (width <= 0 || height <= 0 || _ctx.Target.IsDisposed)
                    continue;

                // SK bitmaps wiederverwenden
                try {
                    if (_bgRenderBitmap == null || _bgRenderBitmap.Width != width || _bgRenderBitmap.Height != height) {
                        try { _bgRenderBitmap?.Dispose(); }
                        catch { }
                        _bgRenderBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                    }
                    if (_uiPresentBitmap == null || _uiPresentBitmap.Width != width || _uiPresentBitmap.Height != height) {
                        try { _uiPresentBitmap?.Dispose(); }
                        catch { }
                        _uiPresentBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                    }
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Buffer alloc error: {ex}");
                    continue;
                }

                // Zeichnen im Hintergrundbuffer
                try {
                    using var surface = SKSurface.Create(_bgRenderBitmap.Info, _bgRenderBitmap.GetPixels(), _bgRenderBitmap.RowBytes);
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    DrawWindowFrame(canvas, new Size(width, height));
                    _ctx.Controls.Draw(canvas);

                    lock (_missingRects) {
                        foreach (var r in _missingRects)
                            DrawMissingImageError(canvas, r);
                        _missingRects.Clear();
                    }

                    if (_showDebugRasterOverlay)
                        DrawDebugRasterOverlay(canvas, new Size(width, height));
                    if (_showPerfOverlay)
                        DrawPerformanceOverlay(canvas, new Size(width, height));

                    RecordFrameTimestamp();
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Background draw error: {ex}");
                }

                // Buffer tauschen
                lock (_swapLock) {
                    (_bgRenderBitmap, _uiPresentBitmap) = (_uiPresentBitmap, _bgRenderBitmap);
                }

                // mapped DIB on UI-Thread
                bool dibReady = false;
                try {
                    _ctx.UiThread.Invoke(new Action(() => {
                        dibReady = EnsureMappedDib(width, height);
                    }));
                }
                catch {
                    dibReady = false;
                }

                if (!dibReady) {
                    // Fallback: falls DIB nicht verfügbar, enqueuen wir normalen UI-Post, der Bitmap kopiert
                    EnqueueUiPresentFallback();
                    continue;
                }

                // Hole Referenz auf toPresent
                SKBitmap? toPresent;
                lock (_swapLock) {
                    toPresent = _uiPresentBitmap;
                    _uiPresentBitmap = null;
                }

                if (toPresent == null)
                    continue;

                // Kopiere SKBitmap direkt in dibPixels (unter dibLock) mit sicheren PeekPixels/Span-Operationen
                bool fallbackNeeded = false;
                lock (_dibLock) {
                    if (_dibPixels == IntPtr.Zero) {
                        fallbackNeeded = true;
                    } else {
                        // Verwende die non-out Variante von PeekPixels
                        SKPixmap pixmap = new();
                        bool hasPixmap = false;
                        try {
                            // Manche SkiaSharp-Versionen erwarten ein SKPixmap-Objekt als Parameter (nicht out)
                            hasPixmap = toPresent.PeekPixels(pixmap);
                        }
                        catch {
                            // Falls PeekPixels diese Signatur nicht unterstützt
                            hasPixmap = false;
                        }

                        if (!hasPixmap || pixmap.GetPixels() == IntPtr.Zero) {
                            // Fallback: falls PeekPixels nicht verfügbar
                            fallbackNeeded = true;
                        } else {
                            int srcStride = pixmap.RowBytes;
                            int srcHeight = pixmap.Height;
                            long srcExpected = (long)srcStride * srcHeight;
                            int dstStride = _dibWidth * 4;
                            long dstExpected = (long)dstStride * srcHeight;
                            long skByteCount = toPresent.ByteCount;

                            if (srcExpected <= 0 || dstExpected <= 0 || skByteCount < srcExpected) {
                                fallbackNeeded = true;
                            } else {
                                // Logging vor der Kopie
                                //Debug.WriteLine($"Copy start: toPresent != null: {toPresent != null}, Width={toPresent?.Width}, Height={toPresent?.Height}, RowBytes={toPresent?.RowBytes}, ByteCount={toPresent?.ByteCount}");
                                try {
                                    unsafe {
                                        byte* srcPtr = (byte*)pixmap.GetPixels();
                                        byte* dstPtr = (byte*)_dibPixels;

                                        Span<byte> srcSpan = new(srcPtr, (int)srcExpected);
                                        Span<byte> dstSpan = new(dstPtr, (int)dstExpected);

                                        if (dstStride > srcStride) {
                                            EnsureZeroRowBuffer(dstStride);
                                            for (int y = 0; y < srcHeight; y++) {
                                                var sRow = srcSpan.Slice(y * srcStride, Math.Min(srcStride, dstStride));
                                                var dRow = dstSpan.Slice(y * dstStride, dstStride);
                                                sRow.CopyTo(dRow);
                                                if (dstStride > sRow.Length)
                                                    dRow[sRow.Length..].Clear();
                                            }
                                        } else if (srcStride == dstStride) {
                                            srcSpan.CopyTo(dstSpan);
                                        } else {
                                            for (int y = 0; y < srcHeight; y++) {
                                                var sRow = srcSpan.Slice(y * srcStride, srcStride);
                                                var dRow = dstSpan.Slice(y * dstStride, dstStride);
                                                sRow[..dstStride].CopyTo(dRow);
                                            }
                                        }

                                        GC.KeepAlive(toPresent);
                                    }
                                }
                                catch (Exception e) {
                                    Debug.WriteLine($"Unexpected exception during pixel copy: {e}");
                                    fallbackNeeded = true;
                                }
                            }
                        }
                    }
                }

                if (fallbackNeeded) {
                    EnqueueUiPresentFallbackWithBitmap(toPresent);
                    continue;
                }

                // Enqueue UI update
                // nur UpdateLayeredWindow, kein weiterer Copy
                bool enqueued = false;
                if (Interlocked.Exchange(ref _invokePending, 1) == 0) {
                    try {
                        _ctx.UiThread.BeginInvoke(new Action(() => {
                            Interlocked.Exchange(ref _invokePending, 0);

                            if (_disposed || _ctx.Target.IsDisposed || !_ctx.Target.IsHandleCreated)
                                return;

                            if (Interlocked.Exchange(ref _isRenderingFlag, 1) == 1)
                                return;

                            var presentSw = Stopwatch.StartNew();
                            try {
                                // Present DIB
                                // unter dibLock, damit Worker nicht gleichzeitig schreibt
                                lock (_dibLock) {
                                    if (_hDib == IntPtr.Zero || _dibPixels == IntPtr.Zero)
                                        return;

                                    IntPtr screenDc = IntPtr.Zero;
                                    IntPtr memDc = IntPtr.Zero;
                                    IntPtr oldObj = IntPtr.Zero;
                                    try {
                                        screenDc = NativeMethods.GetDC(IntPtr.Zero);
                                        if (screenDc == IntPtr.Zero)
                                            return;

                                        memDc = NativeMethods.CreateCompatibleDC(screenDc);
                                        if (memDc == IntPtr.Zero)
                                            return;

                                        oldObj = NativeMethods.SelectObject(memDc, _hDib);

                                        Size size = new(_dibWidth, _dibHeight);
                                        Point source = new(0, 0);
                                        Point topPos = new(_ctx.Target.Left, _ctx.Target.Top);

                                        var blend = new NativeMethods.BLENDFUNCTION {
                                            BlendOp = NativeMethods.AC_SRC_OVER,
                                            BlendFlags = 0,
                                            SourceConstantAlpha = 255,
                                            AlphaFormat = NativeMethods.AC_SRC_ALPHA
                                        };

                                        bool success = NativeMethods.UpdateLayeredWindow(
                                        _ctx.Target.Handle, screenDc, ref topPos, ref size, memDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);

                                        if (!success) {
                                            var err = Marshal.GetLastWin32Error();
                                            LastPresentError = err;
                                            Debug.WriteLine($"UpdateLayeredWindow failed: {err}");
                                        }
                                    }
                                    finally {
                                        if (memDc != IntPtr.Zero)
                                            _ = NativeMethods.SelectObject(memDc, oldObj);
                                        if (memDc != IntPtr.Zero)
                                            _ = NativeMethods.DeleteDC(memDc);
                                        if (screenDc != IntPtr.Zero)
                                            _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                                    }
                                }
                            }
                            catch (Exception ex) {
                                Debug.WriteLine($"Present (DIB) error: {ex}");
                            }
                            finally {
                                presentSw.Stop();
                                LastRenderDurationMs = presentSw.ElapsedMilliseconds;
                                Interlocked.Exchange(ref _isRenderingFlag, 0);
                            }
                        }));
                        enqueued = true;
                    }
                    catch (Exception ex) {
                        Interlocked.Exchange(ref _invokePending, 0);
                        Debug.WriteLine($"BeginInvoke failed: {ex}");
                    }
                }

                if (!enqueued) {
                    Interlocked.Exchange(ref _needsRender, 1);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) {
            Debug.WriteLine($"RenderWorkerLoop fatal error: {ex}");
        }
    }


    // Fallback: falls DIB nicht verfügbar
    // enqueuen wir normalen UI-Post, der Bitmap kopiert
    private void EnqueueUiPresentFallback() {
        SKBitmap? toPresent;
        lock (_swapLock) {
            toPresent = _uiPresentBitmap;
            _uiPresentBitmap = null;
        }
        if (toPresent == null)
            return;
        EnqueueUiPresentFallbackWithBitmap(toPresent);
    }

    // Fallback: nutzt Span-basierte Kopien in ein temporäres Bitmap und ruft Present(bitmap)
    private void EnqueueUiPresentFallbackWithBitmap(SKBitmap? toPresent) {
        if (toPresent is null)
            return;

        bool enqueued = false;
        if (Interlocked.Exchange(ref _invokePending, 1) == 0) {
            try {
                _ctx.UiThread.BeginInvoke(new Action(() => {
                    Interlocked.Exchange(ref _invokePending, 0);
                    if (_disposed || _ctx.Target.IsDisposed || !_ctx.Target.IsHandleCreated)
                        return;
                    if (Interlocked.Exchange(ref _isRenderingFlag, 1) == 1)
                        return;

                    var presentSw = Stopwatch.StartNew();
                    try {
                        Bitmap? tmpBmp = null;
                        try {
                            tmpBmp = new Bitmap(toPresent.Width, toPresent.Height, PixelFormat.Format32bppPArgb);
                            var bmpData = tmpBmp.LockBits(new Rectangle(0, 0, tmpBmp.Width, tmpBmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
                            try {
                                byte* src = (byte*)toPresent.GetPixels();
                                byte* dst = (byte*)bmpData.Scan0;
                                int srcStride = toPresent.RowBytes;
                                int dstStride = bmpData.Stride;
                                long srcExpected = (long)srcStride * toPresent.Height;
                                long dstExpected = (long)dstStride * toPresent.Height;
                                long skByteCount = toPresent.ByteCount;
                                if (src == null || dst == null || skByteCount < srcExpected)
                                    return;

                                Span<byte> srcSpan = new(src, (int)srcExpected);
                                Span<byte> dstSpan = new(dst, (int)dstExpected);

                                if (dstStride > srcStride) {
                                    EnsureZeroRowBuffer(dstStride);
                                    for (int y = 0; y < toPresent.Height; y++) {
                                        var sRow = srcSpan.Slice(y * srcStride, Math.Min(srcStride, dstStride));
                                        var dRow = dstSpan.Slice(y * dstStride, dstStride);
                                        sRow.CopyTo(dRow);
                                        if (dstStride > sRow.Length)
                                            dRow[sRow.Length..].Clear();
                                    }
                                } else if (srcStride == dstStride) {
                                    srcSpan.CopyTo(dstSpan);
                                } else {
                                    for (int y = 0; y < toPresent.Height; y++) {
                                        var sRow = srcSpan.Slice(y * srcStride, srcStride);
                                        var dRow = dstSpan.Slice(y * dstStride, dstStride);
                                        sRow[..dstStride].CopyTo(dRow);
                                    }
                                }
                            }
                            finally { tmpBmp.UnlockBits(bmpData); }

                            Present(tmpBmp);
                        }
                        finally {
                            try { tmpBmp?.Dispose(); }
                            catch { }
                        }
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Fallback present error: {ex}");
                    }
                    finally {
                        presentSw.Stop();
                        LastRenderDurationMs = presentSw.ElapsedMilliseconds;
                        Interlocked.Exchange(ref _isRenderingFlag, 0);
                    }
                }));
                enqueued = true;
            }
            catch (Exception ex) {
                Interlocked.Exchange(ref _invokePending, 0);
                Debug.WriteLine($"BeginInvoke failed (fallback): {ex}");
            }
        }

        if (!enqueued) {
            Interlocked.Exchange(ref _needsRender, 1);
        }
    }

    // Present (für Fallbacks)
    public void Present(Bitmap bitmap, byte opacity = 255) {
        if (_disposed || bitmap == null)
            return;
        if (_ctx.Target.IsDisposed || !_ctx.Target.IsHandleCreated)
            return;

        IntPtr hwnd = _ctx.Target.Handle;
        if (hwnd == IntPtr.Zero)
            return;

        IntPtr screenDc = IntPtr.Zero;
        IntPtr memDc = IntPtr.Zero;
        IntPtr hDibLocal = IntPtr.Zero;
        IntPtr oldObj = IntPtr.Zero;
        IntPtr dibPixelsLocal = IntPtr.Zero;

        try {
            screenDc = NativeMethods.GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
                return;

            memDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memDc == IntPtr.Zero)
                return;

            int width = bitmap.Width;
            int height = bitmap.Height;

            var bmi = new NativeMethods.BITMAPINFO {
                bmiHeader = new NativeMethods.BITMAPINFOHEADER {
                    biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = NativeMethods.BI_RGB,
                    biSizeImage = (uint)(width * height * 4)
                },
                bmiColors = new uint[3]
            };

            hDibLocal = NativeMethods.CreateDIBSection(screenDc, ref bmi, NativeMethods.DIB_RGB_COLORS, out dibPixelsLocal, IntPtr.Zero, 0);
            if (hDibLocal == IntPtr.Zero || dibPixelsLocal == IntPtr.Zero)
                return;

            var bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try {
                byte* src = (byte*)bmpData.Scan0;
                byte* dst = (byte*)dibPixelsLocal;
                int srcStride = bmpData.Stride;
                int dstStride = width * 4;
                long srcExpected = (long)srcStride * height;
                long dstExpected = (long)dstStride * height;

                Span<byte> srcSpan = new(src, (int)srcExpected);
                Span<byte> dstSpan = new(dst, (int)dstExpected);

                if (srcStride == dstStride) {
                    srcSpan.CopyTo(dstSpan);
                } else if (dstStride > srcStride) {
                    for (int y = 0; y < height; y++) {
                        var sRow = srcSpan.Slice(y * srcStride, Math.Min(srcStride, dstStride));
                        var dRow = dstSpan.Slice(y * dstStride, dstStride);
                        sRow.CopyTo(dRow);
                        if (dstStride > sRow.Length)
                            dRow[sRow.Length..].Clear();
                    }
                } else {
                    for (int y = 0; y < height; y++) {
                        var sRow = srcSpan.Slice(y * srcStride, srcStride);
                        var dRow = dstSpan.Slice(y * dstStride, dstStride);
                        sRow[..dstStride].CopyTo(dRow);
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }

            oldObj = NativeMethods.SelectObject(memDc, hDibLocal);

            Size size = new(width, height);
            Point source = new(0, 0);
            Point topPos = new(_ctx.Target.Left, _ctx.Target.Top);

            var blend = new NativeMethods.BLENDFUNCTION {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = opacity,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            bool success = NativeMethods.UpdateLayeredWindow(hwnd, screenDc, ref topPos, ref size, memDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
            if (!success) {
                var err = Marshal.GetLastWin32Error();
                LastPresentError = err;
                Debug.WriteLine($"UpdateLayeredWindow failed: {err}");
            }
        }
        finally {
            if (memDc != IntPtr.Zero)
                _ = NativeMethods.SelectObject(memDc, oldObj);
            if (hDibLocal != IntPtr.Zero)
                _ = NativeMethods.DeleteObject(hDibLocal);
            if (memDc != IntPtr.Zero)
                _ = NativeMethods.DeleteDC(memDc);
            if (screenDc != IntPtr.Zero)
                _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    public void DrawWindowFrame(SKCanvas canvas, Size size) {
        var bg = _ctx.Skin.GetBackground();
        var layout = _ctx.Skin.GetLayout();
        if (bg == null || layout == null)
            return;

        int width = size.Width;
        int height = size.Height;

        _missingRects.Clear();
        canvas.Clear(SKColors.Transparent);

        if (bg.TopLeft != null)
            canvas.DrawBitmap(bg.TopLeft, new SKPoint(0, 0));
        else
            _missingRects.Add(new SKRect(0, 0, Math.Min(64, width / 6f), Math.Min(64, height / 6f)));

        if (bg.TopRight != null)
            canvas.DrawBitmap(bg.TopRight, new SKPoint(width - bg.TopRight.Width, 0));
        else
            _missingRects.Add(new SKRect(width - Math.Min(64, width / 6f), 0, width, Math.Min(64, height / 6f)));

        if (bg.BottomLeft != null)
            canvas.DrawBitmap(bg.BottomLeft, new SKPoint(0, height - bg.BottomLeft.Height));
        else
            _missingRects.Add(new SKRect(0, height - Math.Min(64, height / 6f), Math.Min(64, width / 6f), height));

        if (bg.BottomRight != null)
            canvas.DrawBitmap(bg.BottomRight, new SKPoint(width - bg.BottomRight.Width, height - bg.BottomRight.Height));
        else
            _missingRects.Add(new SKRect(width - Math.Min(64, width / 6f), height - Math.Min(64, height / 6f), width, height));

        if (bg.TopCenter != null) {
            float left = (bg.TopLeft?.Width) ?? 0f;
            float right = left + (width - ((bg.TopLeft?.Width) ?? 0f) - ((bg.TopRight?.Width) ?? 0f) + layout.TopWidthOffset);
            float bottom = bg.TopCenter.Height;
            if (right > left)
                canvas.DrawBitmap(bg.TopCenter, new SKRect(left, 0, right, bottom));
        } else {
            float left = (bg.TopLeft?.Width) ?? 0f;
            float right = left + (width - ((bg.TopLeft?.Width) ?? 0f) - ((bg.TopRight?.Width) ?? 0f) + layout.TopWidthOffset);
            if (right > left)
                _missingRects.Add(new SKRect(left, 0, right, Math.Min(64, height / 6f)));
        }

        if (bg.BottomCenter != null) {
            float left = (bg.BottomLeft?.Width) ?? 0f;
            float top = height - bg.BottomCenter.Height;
            float right = left + (width - ((bg.BottomLeft?.Width) ?? 0f) - ((bg.BottomRight?.Width) ?? 0f) + layout.BottomWidthOffset);
            float bottom = top + bg.BottomCenter.Height;
            if (right > left)
                canvas.DrawBitmap(bg.BottomCenter, new SKRect(left, top, right, bottom));
        } else {
            float left = (bg.BottomLeft?.Width) ?? 0f;
            float top = Math.Max(0, height - Math.Min(64, height / 6f));
            float right = left + (width - ((bg.BottomLeft?.Width) ?? 0f) - ((bg.BottomRight?.Width) ?? 0f) + layout.BottomWidthOffset);
            if (right > left)
                _missingRects.Add(new SKRect(left, top, right, height));
        }

        if (bg.LeftCenter != null) {
            float top = (bg.TopLeft?.Height) ?? 0f;
            float bottom = top + (height - ((bg.TopLeft?.Height) ?? 0f) - ((bg.BottomLeft?.Height) ?? 0f) + layout.LeftHeightOffset);
            if (bottom > top)
                canvas.DrawBitmap(bg.LeftCenter, new SKRect(0, top, bg.LeftCenter.Width, bottom));
        } else {
            float top = (bg.TopLeft?.Height) ?? 0f;
            float bottom = top + (height - ((bg.TopLeft?.Height) ?? 0f) - ((bg.BottomLeft?.Height) ?? 0f) + layout.LeftHeightOffset);
            if (bottom > top)
                _missingRects.Add(new SKRect(0, top, Math.Min(64, size.Width / 6f), bottom));
        }

        if (bg.RightCenter != null) {
            float left = size.Width - bg.RightCenter.Width;
            float top = (bg.TopRight?.Height) ?? 0f;
            float bottom = top + (size.Height - ((bg.TopRight?.Height) ?? 0f) - ((bg.BottomRight?.Height) ?? 0f) + layout.RightHeightOffset);
            if (bottom > top)
                canvas.DrawBitmap(bg.RightCenter, new SKRect(left, top, left + bg.RightCenter.Width, bottom));
        } else {
            float left = size.Width - Math.Min(64, size.Width / 6f);
            float top = (bg.TopRight?.Height) ?? 0f;
            float bottom = top + (size.Height - ((bg.TopRight?.Height) ?? 0f) - ((bg.BottomRight?.Height) ?? 0f) + layout.RightHeightOffset);
            if (bottom > top)
                _missingRects.Add(new SKRect(left, top, size.Width, bottom));
        }

        float leftWidth = (bg.LeftCenter?.Width) ?? 0f;
        float topHeight = (bg.TopCenter?.Height) ?? 0f;
        float bottomHeight = (bg.BottomCenter?.Height) ?? 0f;
        float fillLeft = Math.Max(0f, leftWidth - layout.FillPosOffset);
        float fillTop = Math.Max(0f, topHeight - layout.FillPosOffset);
        float fillRight = fillLeft + Math.Max(0f, size.Width - leftWidth * 2 + layout.FillWidthOffset);
        float fillBottom = fillTop + Math.Max(0f, size.Height - topHeight - bottomHeight + layout.FillHeightOffset);

        if (fillRight > fillLeft && fillBottom > fillTop) {
            var fillRect = new SKRect(fillLeft, fillTop, fillRight, fillBottom);
            if (bg.FillBitmap != null) {
                bool useTileMode = false;
                if (useTileMode) {
                    using var shader = SKShader.CreateBitmap(bg.FillBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
                    using var paint = new SKPaint { Shader = shader, IsAntialias = true };
                    canvas.DrawRect(fillRect, paint);
                } else {
                    canvas.DrawBitmap(bg.FillBitmap, fillRect);
                }
            } else {
                _fillPaint.Color = bg.FillColor.ToSKColor();
                canvas.DrawRect(fillRect, _fillPaint);
            }
        }
    }

    private void DrawDebugRasterOverlay(SKCanvas canvas, Size size) {
        float scale = Math.Max(1f, _ctx.Target.DeviceDpi / 96f);

        int width = size.Width;
        int height = size.Height;

        int basNumberSpacing = 80;
        int numberSpacing = Math.Max(8, (int)(basNumberSpacing * scale));

        int baseRasterSpacing = 25;
        int rasterSpacing = Math.Max(8, (int)(baseRasterSpacing * scale));
        float mouseMarkerSize = 4f;

        using var linePaint = new SKPaint {
            Color = new SKColor(0xFF, 0xFF, 0xFF, 0x28),
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var axisPaint = new SKPaint {
            Color = new SKColor(0xFF, 0xFF, 0x00, 0xFF),
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var textPaint = new SKPaint {
            Color = new SKColor(0, 255, 100, 220),
            IsAntialias = true
        };

        float fontSize = Math.Max(7f, 12f * scale);
        using var font = new SKFont(SKTypeface.Default, fontSize);

        for (int x = 0; x < width; x += rasterSpacing)
            canvas.DrawLine(x, 0, x, height, linePaint);

        for (int y = 0; y < height; y += rasterSpacing)
            canvas.DrawLine(0, y, width, y, linePaint);

        canvas.DrawLine(0, 0, width, 0, axisPaint);
        canvas.DrawLine(0, 0, 0, height, axisPaint);

        var metrics = font.Metrics;
        float baselineOffset = -(metrics.Descent + metrics.Ascent) / 2f;

        for (int x = 0; x < width; x += numberSpacing) {
            string sx = x.ToString();
            canvas.DrawText(sx, x + 2, 2 + baselineOffset + fontSize, SKTextAlign.Left, font, textPaint);
        }

        for (int y = 0; y < height; y += numberSpacing) {
            string sy = y.ToString();
            canvas.DrawText(sy, 2, y + 2 + baselineOffset + fontSize, SKTextAlign.Left, font, textPaint);
        }

        try {
            var cursorScreen = System.Windows.Forms.Cursor.Position;
            int cursorX = cursorScreen.X - _ctx.Target.Left;
            int cursorY = cursorScreen.Y - _ctx.Target.Top;
            if (cursorX >= 0 && cursorX < width && cursorY >= 0 && cursorY < height) {
                using var cursorPaint = new SKPaint { Color = SKColors.Lime, IsAntialias = true };
                canvas.DrawCircle(cursorX, cursorY, mouseMarkerSize * scale, cursorPaint);
                string pos = $"{cursorX},{cursorY}";
                canvas.DrawText(pos, cursorX + 8f * scale, cursorY - 8f * scale, SKTextAlign.Left, font, textPaint);
                RequestRender();
            }
        }
        catch { /* ignore */ }
    }

    private void DrawPerformanceOverlay(SKCanvas canvas, Size size) {
        if (!_showPerfOverlay)
            return;

        long nowMs = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
        double fps = 0;
        int frameCount = 0;
        lock (_perfLock) {
            frameCount = _frameTimestamps.Count;
            if (frameCount >= 2) {
                long first = _frameTimestamps.Peek();
                long last = _frameTimestamps.Last();
                double span = Math.Max(1, last - first);
                fps = (frameCount - 1) * 1000.0 / span;
            }
        }

        long lastRenderMs = LastRenderDurationMs;
        int lastPresentErr = LastPresentError;
        long workingSet = Process.GetCurrentProcess().WorkingSet64 / 1024;
        int threadCount = Process.GetCurrentProcess().Threads.Count;
        int bufW = _renderBuffer?.Width ?? 0;
        int bufH = _renderBuffer?.Height ?? 0;

        using var bgPaint = new SKPaint { Color = new SKColor(0, 0, 0, 140), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var textPaint = new SKPaint { Color = SKColors.Lime, IsAntialias = true };
        using var titlePaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        using var textFont = new SKFont(SKTypeface.FromFamilyName("Consolas"), 12);
        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Consolas"), 13);

        float padding = 48f;
        float lineHeight = 16f;
        float boxWidth = 260f;
        float boxHeight = padding * 2 + lineHeight * 7;

        var rect = new SKRect(padding, padding, padding + boxWidth, padding + boxHeight);
        canvas.DrawRoundRect(rect, 6, 6, bgPaint);

        float x = rect.Left + padding;
        float y = rect.Top + padding + lineHeight;

        void DrawLine(string label, string value) {
            canvas.DrawText(label, x, y, SKTextAlign.Left, titleFont, titlePaint);
            canvas.DrawText(value, x + 120, y, SKTextAlign.Left, textFont, textPaint);
            y += lineHeight;
        }


        DrawLine("FPS", fps > 0 ? $"{fps:F1}" : "—");
        DrawLine("Last Render ms", $"{lastRenderMs} ms");
        DrawLine("Last Present Err", lastPresentErr == 0 ? "OK" : lastPresentErr.ToString());
        DrawLine("RenderBuffer", $"{bufW}x{bufH}");
        DrawLine("Process Mem", $"{workingSet} KB");
        DrawLine("Threads", threadCount.ToString());
        DrawLine("Time", DateTime.Now.ToString("HH:mm:ss"));

        float barMax = boxWidth - 2 * padding;
        float barY = rect.Bottom - padding - 6;
        float normalized = Math.Min(1f, lastRenderMs / 50f);
        using var barBg = new SKPaint { Color = new SKColor(255, 255, 255, 30), IsAntialias = true };
        using var barFill = new SKPaint { Color = normalized < 0.5 ? SKColors.Lime : SKColors.OrangeRed, IsAntialias = true };
        canvas.DrawRect(x, barY, barMax, 6, barBg);
        canvas.DrawRect(x, barY, barMax * normalized, 6, barFill);
    }

    private static void DrawMissingImageError(SKCanvas canvas, SKRect rect) {
        using var bgPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Black, IsAntialias = true };
        using var borderPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Magenta, StrokeWidth = 2, IsAntialias = true };
        using var textPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Magenta, IsAntialias = true };

        float stroke = Math.Max(4f, Math.Min(rect.Width, rect.Height) / 8f);
        using var font = new SKFont(SKTypeface.Default, Math.Max(10f, Math.Max(rect.Width, rect.Height) / 20f));
        var metrics = font.Metrics;

        canvas.DrawRect(rect, bgPaint);
        canvas.DrawRect(rect, borderPaint);

        float cx = rect.MidX;
        float cy = rect.MidY;
        bool rotate = rect.Height > rect.Width;

        canvas.Save();
        canvas.Translate(cx, cy);
        if (rotate)
            canvas.RotateDegrees(-90);
        float baselineY = -(metrics.Descent + metrics.Ascent) / 2f;
        canvas.DrawText("MISSING IMAGE", 0, baselineY, SKTextAlign.Center, font, textPaint);
        canvas.Restore();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;
        _disposed = true;

        try { StopRenderWorker(); }
        catch { }

        _needsRender = 0;
        Interlocked.Exchange(ref _invokePending, 0);
        Interlocked.Exchange(ref _isRenderingFlag, 0);

        if (disposing) {
            try { _renderWorker = null; }
            catch { }
            try { _renderCts.Dispose(); }
            catch { }

            try { _renderSurface?.Dispose(); }
            catch { }
            try { _renderBuffer?.Dispose(); }
            catch { }
            try { _bgRenderBitmap?.Dispose(); }
            catch { }
            try { _uiPresentBitmap?.Dispose(); }
            catch { }
            try { _fillPaint.Dispose(); }
            catch { }
        }

        try {
            ReleaseMappedDib();
        }
        catch { }

        _zeroRowBuffer = null;
    }
}
