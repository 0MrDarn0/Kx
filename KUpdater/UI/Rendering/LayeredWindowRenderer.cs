// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KUpdater.Core;
using KUpdater.Core.Interop;
using SkiaSharp;

namespace KUpdater.UI.Rendering;

public unsafe class LayeredWindowRenderer : IWindowRenderer, IDisposable {
    private readonly WindowContext _ctx;

    // Steuerflags
    private int _needsRender;
    private int _invokePending;
    private int _isRenderingFlag;

    // Worker-thread persistente Skia-Buffer
    private SKBitmap? _bgRenderBitmap;   // Worker zeichnet hier hinein
    private SKSurface? _bgSurface;       // Surface für _bgRenderBitmap

    private SKBitmap? _uiPresentBitmap;  // Wird zum UI-Thread geswapped
    private SKSurface? _uiSurface;       // Surface für _uiPresentBitmap

    private readonly object _swapLock = new();

    // Worker lifecycle
    private readonly CancellationTokenSource _renderCts = new();
    private Task? _renderWorker;
    private volatile bool _workerRunning;

    // DIBSection mit FileMapping (UI-Thread)
    private IntPtr _hSection = IntPtr.Zero;
    private IntPtr _hDib = IntPtr.Zero;
    private IntPtr _dibPixels = IntPtr.Zero;
    private int _dibWidth;
    private int _dibHeight;
    private readonly object _dibLock = new();

    private readonly SKPaint _fillPaint = new() { IsAntialias = true };

    // Fallback-Hilfspuffer
    private byte[]? _zeroRowBuffer;
    private int _zeroRowBufferSize;

    // Debug / Overlay
    private readonly List<SKRect> _missingRects = new();
    private readonly object _perfLock = new();
    private readonly Queue<long> _frameTimestamps = new();
    private const int FrameHistory = 60;

    private bool _showDebugRasterOverlay;
    private bool _showPerfOverlay;
    private bool _showContentRectDebug;

    public long LastRenderDurationMs { get; private set; }
    public int LastPresentError { get; private set; }

    private bool _disposed;

    public LayeredWindowRenderer(WindowContext ctx) {
        _ctx = ctx;
        StartRenderWorker();
    }

    public void ToggleDebugOverlay() => _showDebugRasterOverlay = !_showDebugRasterOverlay;
    public void SetDebugOverlay(bool enabled) => _showDebugRasterOverlay = enabled;

    public void TogglePerfOverlay() => _showPerfOverlay = !_showPerfOverlay;
    public void SetPerfOverlay(bool enabled) => _showPerfOverlay = enabled;

    public void ToggleContentRectDebug() => _showContentRectDebug = !_showContentRectDebug;

    public void RequestRender() => Interlocked.Exchange(ref _needsRender, 1);

    public void Resize(int width, int height) {
        // Worker-Buffer werden im Worker bei Bedarf neu angelegt.
        // DIBSection wird im Present-Pfad angepasst.
    }

    // ============================================================
    //  Dispose
    // ============================================================
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

            try { _bgSurface?.Dispose(); }
            catch { }
            try { _bgRenderBitmap?.Dispose(); }
            catch { }

            try { _uiSurface?.Dispose(); }
            catch { }
            try { _uiPresentBitmap?.Dispose(); }
            catch { }

            try { _fillPaint.Dispose(); }
            catch { }
        }

        try { ReleaseMappedDib(); }
        catch { }

        _bgSurface = null;
        _bgRenderBitmap = null;
        _uiSurface = null;
        _uiPresentBitmap = null;
        _zeroRowBuffer = null;
    }

    // ============================================================
    //  Worker lifecycle
    // ============================================================
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

    // ============================================================
    //  Hilfsfunktionen
    // ============================================================
    private void GetDeviceSize(out int deviceWidth, out int deviceHeight) {
        float scale = Math.Max(1f, _ctx.Target.DeviceDpi / 96f);
        deviceWidth = (int)Math.Ceiling(_ctx.Target.Width * scale);
        deviceHeight = (int)Math.Ceiling(_ctx.Target.Height * scale);
        if (deviceWidth <= 0)
            deviceWidth = 1;
        if (deviceHeight <= 0)
            deviceHeight = 1;
    }

    public SKRect GetContentRect(Size size) {
        var f = _ctx.Frame;

        float leftWidth = f.LeftCenter?.Width ?? 0f;
        float rightWidth = f.RightCenter?.Width ?? 0f;
        float topHeight = f.TopCenter?.Height ?? 0f;
        float bottomHeight = f.BottomCenter?.Height ?? 0f;

        float fillLeft = Math.Max(0f, leftWidth - f.FillPosOffset);
        float fillTop = Math.Max(0f, topHeight - f.FillPosOffset);
        float fillRight = fillLeft + Math.Max(0f, size.Width - leftWidth * 2 + f.FillWidthOffset);
        float fillBottom = fillTop + Math.Max(0f, size.Height - topHeight - bottomHeight + f.FillHeightOffset);

        return new SKRect(fillLeft, fillTop, fillRight, fillBottom);
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

    // Persistente Worker-Buffer
    private void EnsureWorkerBuffers(int width, int height) {
        if (_bgRenderBitmap == null ||
            _bgRenderBitmap.Width != width ||
            _bgRenderBitmap.Height != height) {
            try { _bgSurface?.Dispose(); }
            catch { }
            try { _bgRenderBitmap?.Dispose(); }
            catch { }

            _bgRenderBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _bgSurface = SKSurface.Create(
                _bgRenderBitmap.Info,
                _bgRenderBitmap.GetPixels(),
                _bgRenderBitmap.RowBytes);
        }

        if (_uiPresentBitmap == null ||
            _uiPresentBitmap.Width != width ||
            _uiPresentBitmap.Height != height) {
            try { _uiSurface?.Dispose(); }
            catch { }
            try { _uiPresentBitmap?.Dispose(); }
            catch { }

            _uiPresentBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _uiSurface = SKSurface.Create(
                _uiPresentBitmap.Info,
                _uiPresentBitmap.GetPixels(),
                _uiPresentBitmap.RowBytes);
        }
    }

    // ============================================================
    //  DIBSection (UI-Thread)
    // ============================================================
    private bool EnsureMappedDib(int width, int height) {
        if (width <= 0 || height <= 0)
            return false;

        lock (_dibLock) {
            if (_hDib != IntPtr.Zero &&
                _dibWidth == width &&
                _dibHeight == height &&
                _dibPixels != IntPtr.Zero)
                return true;

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

            long bytes = (long)width * height * 4;
            if (bytes <= 0 || bytes > (long)int.MaxValue * 4L) {
                Debug.WriteLine("Requested DIB too large");
                return false;
            }

            uint sizeLow = (uint)(bytes & 0xFFFFFFFF);
            uint sizeHigh = (uint)((bytes >> 32) & 0xFFFFFFFF);

            IntPtr hSection = NativeMethods.CreateFileMapping(
                NativeMethods.INVALID_HANDLE_VALUE,
                IntPtr.Zero,
                NativeMethods.PAGE_READWRITE,
                sizeHigh,
                sizeLow,
                null);

            if (hSection == IntPtr.Zero) {
                Debug.WriteLine($"CreateFileMapping failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

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
            IntPtr hDib = NativeMethods.CreateDIBSection(
                IntPtr.Zero,
                ref bmi,
                NativeMethods.DIB_RGB_COLORS,
                out dibPixels,
                hSection,
                0);

            if (hDib == IntPtr.Zero || dibPixels == IntPtr.Zero) {
                Debug.WriteLine($"CreateDIBSection failed: {Marshal.GetLastWin32Error()}");
                try { _ = NativeMethods.CloseHandle(hSection); }
                catch { }
                return false;
            }

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

    // ============================================================
    //  RenderWorkerLoop
    // ============================================================
    private void RenderWorkerLoop(CancellationToken ct) {
        try {
            while (!ct.IsCancellationRequested) {
                if (Interlocked.Exchange(ref _needsRender, 0) == 0) {
                    Thread.Sleep(8);
                    continue;
                }

                int width = 0, height = 0;
                try {
                    _ctx.UiThread.Invoke(new Action(() => GetDeviceSize(out width, out height)));
                }
                catch {
                    continue;
                }

                if (width <= 0 || height <= 0 || _ctx.Target.IsDisposed)
                    continue;

                EnsureWorkerBuffers(width, height);

                try {
                    var canvas = _bgSurface!.Canvas;
                    canvas.Clear(SKColors.Transparent);
                    Draw(canvas, new Size(width, height));
                    canvas.Flush();
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Background draw error: {ex}");
                }

                lock (_swapLock) {
                    (_bgRenderBitmap, _uiPresentBitmap) = (_uiPresentBitmap, _bgRenderBitmap);
                    (_bgSurface, _uiSurface) = (_uiSurface, _bgSurface);
                }

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
                    EnqueueUiPresentFallback();
                    continue;
                }

                SKBitmap? toPresent;
                lock (_swapLock) {
                    toPresent = _uiPresentBitmap;
                }

                if (toPresent == null)
                    continue;

                bool fallbackNeeded = false;
                lock (_dibLock) {
                    if (_dibPixels == IntPtr.Zero) {
                        fallbackNeeded = true;
                    } else {
                        SKPixmap pixmap = new();
                        bool hasPixmap = false;
                        try {
                            hasPixmap = toPresent.PeekPixels(pixmap);
                        }
                        catch {
                            hasPixmap = false;
                        }

                        if (!hasPixmap || pixmap.GetPixels() == IntPtr.Zero) {
                            fallbackNeeded = true;
                        } else {
                            int srcStride = pixmap.RowBytes;
                            int srcHeight = pixmap.Height;
                            long srcExpected = (long)srcStride * srcHeight;

                            int dstStride = _dibWidth * 4;
                            long dstExpected = (long)dstStride * srcHeight;

                            if (srcExpected <= 0 || dstExpected <= 0 || toPresent.ByteCount < srcExpected) {
                                fallbackNeeded = true;
                            } else {
                                try {
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
                                }
                                catch (Exception ex) {
                                    Debug.WriteLine($"Pixel copy error: {ex}");
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
                                PresentDib();
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

                if (!enqueued)
                    Interlocked.Exchange(ref _needsRender, 1);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) {
            Debug.WriteLine($"RenderWorkerLoop fatal error: {ex}");
        }
    }

    // ============================================================
    //  Present-Pfade
    // ============================================================
    private void PresentDib() {
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
                    _ctx.Target.Handle,
                    screenDc,
                    ref topPos,
                    ref size,
                    memDc,
                    ref source,
                    0,
                    ref blend,
                    NativeMethods.ULW_ALPHA);

                if (!success) {
                    LastPresentError = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"UpdateLayeredWindow failed: {LastPresentError}");
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

    private void EnqueueUiPresentFallback() {
        SKBitmap? toPresent;
        lock (_swapLock) {
            toPresent = _uiPresentBitmap;
        }
        if (toPresent == null)
            return;

        EnqueueUiPresentFallbackWithBitmap(toPresent);
    }

    private void EnqueueUiPresentFallbackWithBitmap(SKBitmap? toPresent) {
        if (toPresent == null)
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
                        PresentFallbackBitmap(toPresent);
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
                Debug.WriteLine($"BeginInvoke failed (Fallback): {ex}");
            }
        }

        if (!enqueued)
            Interlocked.Exchange(ref _needsRender, 1);
    }

    private void PresentFallbackBitmap(SKBitmap toPresent) {
        Bitmap? tmpBmp = null;
        try {
            tmpBmp = new Bitmap(toPresent.Width, toPresent.Height, PixelFormat.Format32bppPArgb);
            var bmpData = tmpBmp.LockBits(
                new Rectangle(0, 0, tmpBmp.Width, tmpBmp.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppPArgb);

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
            finally {
                tmpBmp.UnlockBits(bmpData);
            }

            Present(tmpBmp);
        }
        finally {
            try { tmpBmp?.Dispose(); }
            catch { }
        }
    }

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

            hDibLocal = NativeMethods.CreateDIBSection(
                screenDc,
                ref bmi,
                NativeMethods.DIB_RGB_COLORS,
                out dibPixelsLocal,
                IntPtr.Zero,
                0);

            if (hDibLocal == IntPtr.Zero || dibPixelsLocal == IntPtr.Zero)
                return;

            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppPArgb);

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
            finally {
                bitmap.UnlockBits(bmpData);
            }

            IntPtr old = NativeMethods.SelectObject(memDc, hDibLocal);

            Size size = new(width, height);
            Point source = new(0, 0);
            Point topPos = new(_ctx.Target.Left, _ctx.Target.Top);

            var blend = new NativeMethods.BLENDFUNCTION {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = opacity,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            bool success = NativeMethods.UpdateLayeredWindow(
                hwnd,
                screenDc,
                ref topPos,
                ref size,
                memDc,
                ref source,
                0,
                ref blend,
                NativeMethods.ULW_ALPHA);

            if (!success) {
                LastPresentError = Marshal.GetLastWin32Error();
                Debug.WriteLine($"UpdateLayeredWindow (fallback) failed: {LastPresentError}");
            }

            _ = NativeMethods.SelectObject(memDc, old);
        }
        finally {
            if (hDibLocal != IntPtr.Zero)
                _ = NativeMethods.DeleteObject(hDibLocal);
            if (memDc != IntPtr.Zero)
                _ = NativeMethods.DeleteDC(memDc);
            if (screenDc != IntPtr.Zero)
                _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    // ============================================================
    //  Draw + Overlays
    // ============================================================
    private void Draw(SKCanvas canvas, Size size) {
        RecordFrameTimestamp();

        DrawWindowFrame(canvas, size);

        try {
            _ctx.Controls.DrawFrameControls(canvas);
            _ctx.ContentRoot.Draw(canvas);
            _ctx.Controls.DrawOverlayControls(canvas);
        }
        catch (Exception ex) {
            Debug.WriteLine($"UI render error: {ex}");
        }

        if (_showDebugRasterOverlay)
            DrawDebugRaster(canvas, size);

        if (_showPerfOverlay)
            DrawPerfOverlay(canvas, size);

        if (_showContentRectDebug)
            DrawContentRectDebug(canvas, size);
    }

    private void DrawWindowFrame(SKCanvas canvas, Size size) {
        var frame = _ctx.Frame;
        if (frame == null)
            return;

        int width = size.Width;
        int height = size.Height;

        canvas.Clear(SKColors.Transparent);

        if (frame.TopLeft != null)
            canvas.DrawBitmap(frame.TopLeft, new SKPoint(0, 0));

        if (frame.TopRight != null)
            canvas.DrawBitmap(frame.TopRight, new SKPoint(width - frame.TopRight.Width, 0));

        if (frame.BottomLeft != null)
            canvas.DrawBitmap(frame.BottomLeft, new SKPoint(0, height - frame.BottomLeft.Height));

        if (frame.BottomRight != null)
            canvas.DrawBitmap(frame.BottomRight, new SKPoint(width - frame.BottomRight.Width, height - frame.BottomRight.Height));

        if (frame.TopCenter != null) {
            float left = frame.TopLeft?.Width ?? 0f;
            float right = left + (width - (frame.TopLeft?.Width ?? 0f) - (frame.TopRight?.Width ?? 0f) + frame.TopWidthOffset);
            float bottom = frame.TopCenter.Height;
            if (right > left)
                canvas.DrawBitmap(frame.TopCenter, new SKRect(left, 0, right, bottom));
        }

        if (frame.BottomCenter != null) {
            float left = frame.BottomLeft?.Width ?? 0f;
            float top = height - frame.BottomCenter.Height;
            float right = left + (width - (frame.BottomLeft?.Width ?? 0f) - (frame.BottomRight?.Width ?? 0f) + frame.BottomWidthOffset);
            float bottom = top + frame.BottomCenter.Height;
            if (right > left)
                canvas.DrawBitmap(frame.BottomCenter, new SKRect(left, top, right, bottom));
        }

        if (frame.LeftCenter != null) {
            float top = frame.TopLeft?.Height ?? 0f;
            float bottom = top + (height - (frame.TopLeft?.Height ?? 0f) - (frame.BottomLeft?.Height ?? 0f) + frame.LeftHeightOffset);
            if (bottom > top)
                canvas.DrawBitmap(frame.LeftCenter, new SKRect(0, top, frame.LeftCenter.Width, bottom));
        }

        if (frame.RightCenter != null) {
            float left = width - frame.RightCenter.Width;
            float top = frame.TopRight?.Height ?? 0f;
            float bottom = top + (height - (frame.TopRight?.Height ?? 0f) - (frame.BottomRight?.Height ?? 0f) + frame.RightHeightOffset);
            if (bottom > top)
                canvas.DrawBitmap(frame.RightCenter, new SKRect(left, top, left + frame.RightCenter.Width, bottom));
        }

        float leftW = frame.LeftCenter?.Width ?? 0f;
        float topH = frame.TopCenter?.Height ?? 0f;
        float bottomH = frame.BottomCenter?.Height ?? 0f;

        float fillLeft = Math.Max(0f, leftW - frame.FillPosOffset);
        float fillTop = Math.Max(0f, topH - frame.FillPosOffset);
        float fillRight = fillLeft + Math.Max(0f, width - leftW * 2 + frame.FillWidthOffset);
        float fillBottom = fillTop + Math.Max(0f, height - topH - bottomH + frame.FillHeightOffset);

        if (fillRight > fillLeft && fillBottom > fillTop) {
            var fillRect = new SKRect(fillLeft, fillTop, fillRight, fillBottom);
            if (frame.FillBitmap != null) {
                canvas.DrawBitmap(frame.FillBitmap, fillRect);
            } else {
                _fillPaint.Color = frame.FillColor;
                canvas.DrawRect(fillRect, _fillPaint);
            }
        }
    }

    private void DrawDebugRaster(SKCanvas canvas, Size size) {
        using var p = new SKPaint {
            Color = new SKColor(255, 0, 0, 40),
            StrokeWidth = 1,
            IsStroke = true
        };

        for (int x = 0; x < size.Width; x += 20)
            canvas.DrawLine(x, 0, x, size.Height, p);

        for (int y = 0; y < size.Height; y += 20)
            canvas.DrawLine(0, y, size.Width, y, p);
    }

    private void DrawPerfOverlay(SKCanvas canvas, Size size) {
        long now = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

        long fps;
        lock (_perfLock) {
            while (_frameTimestamps.Count > 0 && now - _frameTimestamps.Peek() > 1000)
                _frameTimestamps.Dequeue();
            fps = _frameTimestamps.Count;
        }

        using var paint = new SKPaint {
            Color = SKColors.Lime,
            TextSize = 16,
            IsAntialias = true
        };

        canvas.DrawText($"FPS: {fps}", 10, 20, paint);
        canvas.DrawText($"Render: {LastRenderDurationMs} ms", 10, 40, paint);
    }

    private void DrawContentRectDebug(SKCanvas canvas, Size size) {
        var rect = GetContentRect(size);

        using var p = new SKPaint {
            Color = new SKColor(0, 255, 255, 80),
            IsStroke = true,
            StrokeWidth = 2
        };

        canvas.DrawRect(rect, p);
    }
}
