// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Kx.App;
using Kx.Core.Interop;
using Kx.Core.Interop.SafeHandles;
using Kx.Sdk.Rendering;
using Kx.UI.Themes;
using Kx.Utility;

using SkiaSharp;

namespace Kx.UI.Rendering;

public unsafe class LayeredWindowRenderer : IWindowRenderer, IDisposable {
    private readonly WindowContext _ctx;

    // Steuerflags
    private int _needsRender;
    private int _invokePending;
    private int _isRenderingFlag;

    // Worker-thread persistente Skia-Buffer
    private SKBitmap? _bgRenderBitmap;
    private SKSurface? _bgSurface;

    private SKBitmap? _uiPresentBitmap;
    private SKSurface? _uiSurface;

    private readonly object _swapLock = new();

    // Worker lifecycle
    private readonly CancellationTokenSource _renderCts = new();
    private Task? _renderWorker;
    private volatile bool _workerRunning;

    // DIBSection + DC (UI-Thread)
    private IntPtr _hSection = IntPtr.Zero;
    private SafeGdiObjectHandle? _hDibHandle;
    private IntPtr _dibPixels = IntPtr.Zero;
    private int _dibWidth;
    private int _dibHeight;
    private readonly object _dibLock = new();

    // Persistenter Memory-DC für UpdateLayeredWindow
    private SafeMemoryDcHandle? _memDc;
    private IntPtr _oldMemDcObj = IntPtr.Zero;

    // Fallback-Hilfspuffer
    private byte[]? _zeroRowBuffer;

    // Debug / Overlay
    private readonly RenderOverlayRegistry _overlayRegistry = new();

    public long LastRenderDurationMs { get; private set; }
    public int LastPresentError { get; private set; }

    private bool _disposed;

    public LayeredWindowRenderer(WindowContext ctx) {
        _ctx = ctx;
        InitializeOverlays();
        StartRenderWorker();
    }

    public void ToggleDebugOverlay() => ToggleOverlay(DebugRasterOverlay.OverlayId);
    public void TogglePerfOverlay() => ToggleOverlay(PerformanceOverlay.OverlayId);
    public void ToggleContentRectDebug() => ToggleOverlay(ContentRectOverlay.OverlayId);
    public void RequestRender() => Interlocked.Exchange(ref _needsRender, 1);

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

        // dispose früher erstellter Skia-Objekte und sichere Nullsetzung
        if (disposing) {
            try { _renderWorker = null; }
            catch { }
            try { _renderCts.Dispose(); }
            catch { }

            try {
                try { _overlayRegistry.Dispose(); }
                catch { }
                try { _bgSurface?.Dispose(); }
                catch { }
                try { _bgRenderBitmap?.Dispose(); }
                catch { }
                try { _uiSurface?.Dispose(); }
                catch { }
                try { _uiPresentBitmap?.Dispose(); }
                catch { }
            }
            finally {
                _bgSurface = null;
                _bgRenderBitmap = null;
                _uiSurface = null;
                _uiPresentBitmap = null;
                _zeroRowBuffer = null;
            }
        }

        try { ReleaseMappedDib(); }
        catch { }

        try {
            lock (_dibLock) {
                if (_memDc is not null && !_memDc.IsInvalid) {
                    try {
                        if (_oldMemDcObj != IntPtr.Zero) {
                            TryRestoreSelectedObject(_memDc, _oldMemDcObj);
                            _oldMemDcObj = IntPtr.Zero;
                        }
                    }
                    catch { /* best effort */ }
                }
            }
        }
        catch { /* best effort */ }

        try {
            if (_memDc is not null) {
                try { _memDc.Dispose(); }
                catch { }
                _memDc = null;
            }
        }
        catch { /* best effort */ }
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

    // Sichert Lebenszeit von memDcHandle und hbitmapHandle, selektiert hbitmap und liefert das vorherige Objekt.
    private static bool TrySelectHbitmap(SafeHandle memDcHandle, SafeHandle hbitmapHandle, out IntPtr previous) {
        previous = IntPtr.Zero;
        if (memDcHandle is null || memDcHandle.IsInvalid)
            return false;
        if (hbitmapHandle is null || hbitmapHandle.IsInvalid)
            return false;

        bool memAdded = false;
        bool bmpAdded = false;
        try {
            memDcHandle.DangerousAddRef(ref memAdded);
            hbitmapHandle.DangerousAddRef(ref bmpAdded);

            IntPtr memHdc = memDcHandle.DangerousGetHandle();
            IntPtr bmpH = hbitmapHandle.DangerousGetHandle();

            previous = NativeMethods.SelectObject(memHdc, bmpH);
            return previous != IntPtr.Zero;
        }
        finally {
            if (bmpAdded)
                hbitmapHandle.DangerousRelease();
            if (memAdded)
                memDcHandle.DangerousRelease();
        }
    }

    // Stellt ein zuvor gespeichertes Objekt wieder her; sichert Lebenszeit des DC SafeHandle.
    private static void TryRestoreSelectedObject(SafeHandle memDcHandle, IntPtr previous) {
        if (previous == IntPtr.Zero)
            return;
        if (memDcHandle is null || memDcHandle.IsInvalid)
            return;

        bool memAdded = false;
        try {
            memDcHandle.DangerousAddRef(ref memAdded);
            IntPtr memHdc = memDcHandle.DangerousGetHandle();
            _ = NativeMethods.SelectObject(memHdc, previous);
        }
        catch { /* best effort */ }
        finally {
            if (memAdded)
                memDcHandle.DangerousRelease();
        }
    }


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
    //  DIBSection + persistenter DC (UI-Thread)
    // ============================================================
    private bool EnsureMappedDib(int width, int height) {
        if (width <= 0 || height <= 0)
            return false;

        lock (_dibLock) {
            bool sizeMatches =
            _hDibHandle is not null &&
            !_hDibHandle.IsInvalid &&
            _dibPixels != IntPtr.Zero &&
            _dibWidth == width &&
            _dibHeight == height;

            if (sizeMatches && _memDc is not null && !_memDc.IsInvalid)
                return true;

            // Wenn ein altes DIB existiert: zuerst Rückselektion, dann Dispose und Nulling
            try {
                if (_hDibHandle is not null) {
                    try {
                        if (_memDc is not null && !_memDc.IsInvalid && _oldMemDcObj != IntPtr.Zero) {
                            TryRestoreSelectedObject(_memDc, _oldMemDcObj);
                            _oldMemDcObj = IntPtr.Zero;
                        }
                    }
                    catch { /* best effort */ }

                    try { _hDibHandle.Dispose(); }
                    catch { }
                    _hDibHandle = null;

                    // sofort ungültig markieren
                    _dibPixels = IntPtr.Zero;
                    _dibWidth = 0;
                    _dibHeight = 0;
                }
            }
            catch { /* best effort */ }

            // Close previous section if any
            try {
                if (_hSection != IntPtr.Zero) {
                    _ = NativeMethods.CloseHandle(_hSection);
                    _hSection = IntPtr.Zero;
                }
            }
            catch { /* best effort */ }

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

            // Assign new section and DIB handle
            _hSection = hSection;

            // Dispose any previous handle (already disposed above, but keep defensive)
            try { _hDibHandle?.Dispose(); }
            catch { }
            _hDibHandle = new SafeGdiObjectHandle(hDib);

            _dibPixels = dibPixels;
            _dibWidth = width;
            _dibHeight = height;

            // Persistenten Memory-DC anlegen (falls noch nicht vorhanden)
            if (_memDc is null || _memDc.IsInvalid) {
                IntPtr screenDc = IntPtr.Zero;
                try {
                    screenDc = NativeMethods.GetDC(IntPtr.Zero);
                    if (screenDc == IntPtr.Zero) {
                        Debug.WriteLine("GetDC failed for persistent mem DC");
                        return true; // DIB existiert trotzdem
                    }

                    var memDc = NativeMethods.CreateCompatibleDC(screenDc);
                    if (memDc == IntPtr.Zero) {
                        Debug.WriteLine("CreateCompatibleDC failed for persistent mem DC");
                        return true;
                    }

                    _memDc = new SafeMemoryDcHandle(memDc);
                }
                finally {
                    if (screenDc != IntPtr.Zero)
                        _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                }
            }

            // DIB in persistenten DC selektieren (sicher)
            if (_memDc is not null && !_memDc.IsInvalid && _hDibHandle is not null && !_hDibHandle.IsInvalid) {
                try {
                    if (TrySelectHbitmap(_memDc, _hDibHandle, out IntPtr prev)) {
                        if (_oldMemDcObj == IntPtr.Zero)
                            _oldMemDcObj = prev;
                        else if (prev == IntPtr.Zero)
                            Debug.WriteLine("SelectObject returned null when selecting new DIB into persistent DC.");
                    } else {
                        Debug.WriteLine("TrySelectHbitmap failed when selecting new DIB into persistent DC.");
                    }
                }
                catch (Exception ex) {
                    Debug.WriteLine($"SelectObject persistent mem DC failed: {ex}");
                }
            }

            return true;
        }
    }

    private unsafe bool CopySkBitmapToMappedDib(SKBitmap toPresent) {
        if (toPresent == null)
            return false;

        lock (_dibLock) {
            if (_dibPixels == IntPtr.Zero)
                return false;

            SKPixmap pixmap = new();
            bool hasPixmap = false;
            try {
                hasPixmap = toPresent.PeekPixels(pixmap);
            }
            catch {
                hasPixmap = false;
            }

            if (!hasPixmap || pixmap.GetPixels() == IntPtr.Zero)
                return false;

            int dstStride = _dibWidth * 4;

            // PixelCopyHelpers übernimmt alle Stride-Fälle und nutzt _zeroRowBuffer per ref
            bool ok = PixelCopyHelpers.TryCopyFromSkPixmap(pixmap, _dibPixels, dstStride, ref _zeroRowBuffer);
            return ok;
        }
    }

    private void ReleaseMappedDib() {
        lock (_dibLock) {
            try {
                if (_hDibHandle is not null) {
                    try {
                        if (_memDc is not null && !_memDc.IsInvalid && _oldMemDcObj != IntPtr.Zero) {
                            TryRestoreSelectedObject(_memDc, _oldMemDcObj);
                            _oldMemDcObj = IntPtr.Zero;
                        }
                    }
                    catch { /* best effort */ }

                    try {
                        _hDibHandle.Dispose();
                    }
                    catch { /* best effort */ }

                    _hDibHandle = null;
                    _dibPixels = IntPtr.Zero;
                    _dibWidth = 0;
                    _dibHeight = 0;
                }
            }
            catch { /* best effort */ }

            try {
                if (_hSection != IntPtr.Zero) {
                    _ = NativeMethods.CloseHandle(_hSection);
                    _hSection = IntPtr.Zero;
                }
            }
            catch { /* best effort */ }
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

                // Neuer, zentraler Kopierpfad: versucht, SKBitmap direkt in die gemappte DIB zu kopieren.
                // CopySkBitmapToMappedDib sperrt intern _dibLock und verwendet PixelCopyHelpers.
                try {
                    if (!CopySkBitmapToMappedDib(toPresent))
                        fallbackNeeded = true;
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Pixel copy (central) error: {ex}");
                    fallbackNeeded = true;
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
                                PresentDibFast();
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
    private void PresentDibFast() {
        lock (_dibLock) {
            if (_hDibHandle is null || _hDibHandle.IsInvalid)
                return;
            if (_dibPixels == IntPtr.Zero)
                return;
            if (_memDc is null || _memDc.IsInvalid)
                return;

            IntPtr hwnd = _ctx.Target.Handle;
            if (hwnd == IntPtr.Zero)
                return;

            Size size = new(_dibWidth, _dibHeight);
            Point source = new(0, 0);
            Point topPos = new(_ctx.Target.Left, _ctx.Target.Top);

            var blend = new NativeMethods.BLENDFUNCTION {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            bool memAdded = false;
            try {
                _memDc.DangerousAddRef(ref memAdded);
                IntPtr memHdc = _memDc.DangerousGetHandle();

                bool success = NativeMethods.UpdateLayeredWindow(
                hwnd,
                IntPtr.Zero,
                ref topPos,
                ref size,
                memHdc,
                ref source,
                0,
                ref blend,
                NativeMethods.ULW_ALPHA);

                if (!success) {
                    LastPresentError = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"UpdateLayeredWindow failed: {LastPresentError}");
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"PresentDibFast error: {ex}");
            }
            finally {
                if (memAdded)
                    _memDc.DangerousRelease();
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


    private unsafe void PresentFallbackBitmap(SKBitmap toPresent) {
        if (toPresent == null)
            return;

        Bitmap? tmpBmp = null;
        try {
            tmpBmp = new Bitmap(toPresent.Width, toPresent.Height, PixelFormat.Format32bppPArgb);
            var bmpData = tmpBmp.LockBits(
            new Rectangle(0, 0, tmpBmp.Width, tmpBmp.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppPArgb);

            try {
                SKPixmap pixmap = new();
                bool hasPixmap = false;
                try { hasPixmap = toPresent.PeekPixels(pixmap); }
                catch { hasPixmap = false; }

                if (!hasPixmap || pixmap.GetPixels() == IntPtr.Zero)
                    return;

                int dstStride = bmpData.Stride;
                int rowBytesToCopy = Math.Min(pixmap.RowBytes, dstStride);

                // zentrale Copy-Routine
                PixelCopyHelpers.CopyStrideAware(
                    (byte*)pixmap.GetPixels(),
                    pixmap.RowBytes,
                    (byte*)bmpData.Scan0,
                    dstStride,
                    rowBytesToCopy,
                    toPresent.Height,
                    ref _zeroRowBuffer);
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

    public unsafe void Present(Bitmap bitmap, byte opacity = 255) {
        if (_disposed || bitmap == null)
            return;
        if (_ctx.Target.IsDisposed || !_ctx.Target.IsHandleCreated)
            return;

        IntPtr hwnd = _ctx.Target.Handle;
        if (hwnd == IntPtr.Zero)
            return;

        IntPtr screenDc = IntPtr.Zero;
        IntPtr rawMemDc = IntPtr.Zero;
        IntPtr dibPixelsLocal = IntPtr.Zero;

        SafeMemoryDcHandle? localMemDcHandle = null;
        SafeGdiObjectHandle? localDibHandle = null;
        IntPtr oldObj = IntPtr.Zero;

        try {
            screenDc = NativeMethods.GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
                return;

            rawMemDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (rawMemDc == IntPtr.Zero)
                return;

            localMemDcHandle = new SafeMemoryDcHandle(rawMemDc);

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

            IntPtr hDibLocal = NativeMethods.CreateDIBSection(
            screenDc,
            ref bmi,
            NativeMethods.DIB_RGB_COLORS,
            out dibPixelsLocal,
            IntPtr.Zero,
            0);

            if (hDibLocal == IntPtr.Zero || dibPixelsLocal == IntPtr.Zero)
                return;

            localDibHandle = new SafeGdiObjectHandle(hDibLocal);

            var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppPArgb);

            try {
                int dstStride = width * 4;
                int rowBytesToCopy = Math.Min(bmpData.Stride, dstStride);

                PixelCopyHelpers.CopyFromBitmapData(
                    bmpData.Scan0,
                    bmpData.Stride,
                    dibPixelsLocal,
                    dstStride,
                    rowBytesToCopy,
                    height,
                    ref _zeroRowBuffer);
            }
            finally {
                bitmap.UnlockBits(bmpData);
            }

            bool memAdded = false;
            bool dibAdded = false;
            try {
                localMemDcHandle.DangerousAddRef(ref memAdded);
                localDibHandle.DangerousAddRef(ref dibAdded);

                IntPtr memHdc = localMemDcHandle.DangerousGetHandle();
                IntPtr dibHbitmap = localDibHandle.DangerousGetHandle();

                oldObj = NativeMethods.SelectObject(memHdc, dibHbitmap);

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
                memHdc,
                ref source,
                0,
                ref blend,
                NativeMethods.ULW_ALPHA);

                if (!success) {
                    LastPresentError = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"UpdateLayeredWindow (fallback) failed: {LastPresentError}");
                }
            }
            finally {
                try {
                    if (localMemDcHandle is not null && oldObj != IntPtr.Zero)
                        TryRestoreSelectedObject(localMemDcHandle, oldObj);
                }
                catch { /* best effort */ }

                if (dibAdded)
                    localDibHandle?.DangerousRelease();
                if (memAdded)
                    localMemDcHandle?.DangerousRelease();
            }
        }
        finally {
            try { localDibHandle?.Dispose(); }
            catch { }

            try {
                if (rawMemDc != IntPtr.Zero) {
                    if (localMemDcHandle is not null) {
                        try { localMemDcHandle.Dispose(); }
                        catch { }
                    } else {
                        try { NativeMethods.DeleteDC(rawMemDc); }
                        catch { }
                    }
                }
            }
            catch { }

            try { if (screenDc != IntPtr.Zero) _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc); }
            catch { }
        }
    }


    // ============================================================
    //  Draw + Overlays
    // ============================================================
    private void Draw(SKCanvas canvas, Size size) {
        var renderCanvas = new SkiaCanvas(canvas);

        DrawWindowFrame(canvas, size);

        try {
            _ctx.UIElementManager.LayoutAll(_ctx.Frame!.GetContentRect(size), new SKRect(0, 0, size.Width, size.Height));
            _ctx.UIElementManager.Render(renderCanvas);
        }
        catch (Exception ex) {
            Debug.WriteLine($"UI render error: {ex}");
        }

        var overlayContext = new RenderOverlayContext(
            _ctx,
            _ctx.Frame,
            canvas,
            size,
            LastRenderDurationMs,
            _bgRenderBitmap?.Width ?? 0,
            _bgRenderBitmap?.Height ?? 0,
            RequestRender);

        _overlayRegistry.DrawOverlays(overlayContext);
    }

    private void DrawWindowFrame(SKCanvas canvas, Size size) {
        var f = _ctx.Frame;
        if (f == null)
            return;

        if (f.UsesDefaultFrame) {
            DrawDefaultWindowFrame(canvas, size, f);
            return;
        }

        // Falls Bitmaps nicht exsitieren, platzhalter erstellen
        f.AutoGenerateMissingParts(size.Width, size.Height);

        int w = size.Width;
        int h = size.Height;

        canvas.Clear(SKColors.Transparent);

        // Ecken
        canvas.DrawBitmap(f.TopLeft!, new SKPoint(0, 0));
        canvas.DrawBitmap(f.TopRight!, new SKPoint(w - f.TopRight!.Width, 0));
        canvas.DrawBitmap(f.BottomLeft!, new SKPoint(0, h - f.BottomLeft!.Height));
        canvas.DrawBitmap(f.BottomRight!, new SKPoint(w - f.BottomRight!.Width, h - f.BottomRight!.Height));

        // Top Center
        {
            float left = f.TopLeft!.Width;
            float right = w - f.TopRight!.Width + f.TopWidthOffset;
            float bottom = f.TopCenter!.Height;
            if (right > left)
                canvas.DrawBitmap(f.TopCenter!, new SKRect(left, 0, right, bottom));
        }

        // Bottom Center
        {
            float left = f.BottomLeft!.Width;
            float top = h - f.BottomCenter!.Height;
            float right = w - f.BottomRight!.Width + f.BottomWidthOffset;
            float bottom = top + f.BottomCenter!.Height;
            if (right > left)
                canvas.DrawBitmap(f.BottomCenter!, new SKRect(left, top, right, bottom));
        }

        // Left Center
        {
            float top = f.TopLeft!.Height;
            float bottom = h - f.BottomLeft!.Height + f.LeftHeightOffset;
            if (bottom > top)
                canvas.DrawBitmap(f.LeftCenter!, new SKRect(0, top, f.LeftCenter!.Width, bottom));
        }

        // Right Center
        {
            float left = w - f.RightCenter!.Width;
            float top = f.TopRight!.Height;
            float bottom = h - f.BottomRight!.Height + f.RightHeightOffset;
            if (bottom > top)
                canvas.DrawBitmap(f.RightCenter!, new SKRect(left, top, left + f.RightCenter!.Width, bottom));
        }

        // Fill Content Area
        {
            var rect = _ctx.Frame!.GetContentRect(size);
            if (f.UseFillColor) {
                using var paint = new SKPaint {
                    IsAntialias = true,
                    Color = f.FillColor
                };
                canvas.DrawRect(rect, paint);
            } else {
                canvas.DrawBitmap(f.FillBitmap, rect);
            }
        }
    }

    private void DrawDefaultWindowFrame(SKCanvas canvas, Size size, FrameResource frame) {
        int width = size.Width;
        int height = size.Height;

        canvas.Clear(SKColors.Transparent);

        var outerRect = new SKRect(0, 0, width, height);
        float radius = frame.DefaultFrame.CornerRadius;
        var outerRoundRect = new SKRoundRect(outerRect, radius, radius);

        using var borderPaint = new SKPaint {
            Color = frame.DefaultFrame.BorderColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(outerRoundRect, borderPaint);

        float borderThickness = frame.DefaultFrame.BorderThickness;
        var innerRect = new SKRect(
            outerRect.Left + borderThickness,
            outerRect.Top + borderThickness,
            Math.Max(outerRect.Left + borderThickness, outerRect.Right - borderThickness),
            Math.Max(outerRect.Top + borderThickness, outerRect.Bottom - borderThickness));
        float innerRadius = Math.Max(0, radius - borderThickness);
        var innerRoundRect = new SKRoundRect(innerRect, innerRadius, innerRadius);

        using var bodyPaint = new SKPaint {
            Color = frame.DefaultFrame.BackgroundColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(innerRoundRect, bodyPaint);

        using (new SKAutoCanvasRestore(canvas, true)) {
            canvas.ClipRoundRect(innerRoundRect, antialias: true);

            var titleBarRect = frame.GetTitleBarRect(size);
            using var titleBarPaint = new SKPaint {
                Color = frame.DefaultFrame.TitleBarColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(titleBarRect, titleBarPaint);

            using var separatorPaint = new SKPaint {
                Color = frame.DefaultFrame.SeparatorColor,
                IsAntialias = true,
                StrokeWidth = Math.Max(1f, borderThickness)
            };
            canvas.DrawLine(titleBarRect.Left, titleBarRect.Bottom, titleBarRect.Right, titleBarRect.Bottom, separatorPaint);
        }

        var closeRect = frame.GetCloseButtonRect(size);
        using var closeButtonPaint = new SKPaint {
            Color = frame.DefaultFrame.CloseButtonColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(closeRect, Math.Min(closeRect.Width, closeRect.Height) / 4f, Math.Min(closeRect.Width, closeRect.Height) / 4f, closeButtonPaint);

        using var closeGlyphPaint = new SKPaint {
            Color = frame.DefaultFrame.CloseButtonForegroundColor,
            IsAntialias = true,
            StrokeWidth = Math.Max(1.5f, frame.DefaultFrame.BorderThickness + 0.5f),
            StrokeCap = SKStrokeCap.Round
        };
        float glyphInset = Math.Max(5f, closeRect.Width * 0.28f);
        canvas.DrawLine(closeRect.Left + glyphInset, closeRect.Top + glyphInset, closeRect.Right - glyphInset, closeRect.Bottom - glyphInset, closeGlyphPaint);
        canvas.DrawLine(closeRect.Right - glyphInset, closeRect.Top + glyphInset, closeRect.Left + glyphInset, closeRect.Bottom - glyphInset, closeGlyphPaint);

        string fallbackTitle = Process.GetCurrentProcess().ProcessName;
        string title = frame.GetTitle(fallbackTitle);
        var titleBar = frame.GetTitleBarRect(size);
        float titleLeft = titleBar.Left + frame.DefaultFrame.TitlePadding;
        float titleRight = Math.Max(titleLeft, closeRect.Left - frame.DefaultFrame.TitlePadding);
        var titleRect = new SKRect(titleLeft, titleBar.Top, titleRight, titleBar.Bottom);

        using var titlePaint = new SKPaint {
            Color = frame.DefaultFrame.TitleColor,
            IsAntialias = true
        };
        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), frame.DefaultFrame.TitleFontSize);
        var titleMetrics = titleFont.Metrics;
        float titleBaseline = titleBar.MidY - (titleMetrics.Ascent + titleMetrics.Descent) / 2f;

        using (new SKAutoCanvasRestore(canvas, true)) {
            canvas.ClipRect(titleRect);
            canvas.DrawText(title, titleRect.Left, titleBaseline, SKTextAlign.Left, titleFont, titlePaint);
        }
    }

    private void InitializeOverlays() {
        _overlayRegistry.Register(new DebugRasterOverlay());
        _overlayRegistry.Register(new PerformanceOverlay());
        _overlayRegistry.Register(new ContentRectOverlay());
    }

    private void ToggleOverlay(string overlayId) {
        _overlayRegistry.Toggle(overlayId);
        RequestRender();
    }
}
