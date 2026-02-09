// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Buffers;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KUpdater.Core;
using KUpdater.Extensions;
using KUpdater.Interop;
using KUpdater.UI.Interface;
using SkiaSharp;

namespace KUpdater.UI;

public class Renderer : IRenderer {
    private readonly WindowContext _ctx;
    private readonly System.Windows.Forms.Timer _renderTimer;
    private int _needsRender;
    private SKBitmap? _renderBuffer;
    private SKSurface? _renderSurface;
    private Bitmap? _backBuffer;
    private bool _disposed;
    private readonly SKPaint _fillPaint = new() { IsAntialias = true };

    // Sammlung der fehlenden Bereiche, die als Overlay (topmost) gezeichnet werden sollen
    private readonly List<SKRect> _missingRects = [];

    public bool IsRendering { get; private set; }
    public long LastRenderDurationMs { get; private set; }
    public int LastPresentError { get; private set; }

    public Renderer(WindowContext ctx) {
        _ctx = ctx;
        _renderTimer = new System.Windows.Forms.Timer { Interval = _ctx.Config.RenderTimerInterval };
        _renderTimer.Tick += RenderTimer_Tick;
        _renderTimer.Start();
    }

    public void RequestRender()
        => Interlocked.Exchange(ref _needsRender, 1);

    private void RenderTimer_Tick(object? sender, EventArgs e)
        => RenderTick();

    public void Resize(int width, int height)
        => EnsureBuffers(width, height);

    private void RenderTick() {
        if (Interlocked.Exchange(ref _needsRender, 0) == 0)
            return;
        if (_disposed || _ctx.Target.IsDisposed)
            return;

        if (_ctx.UiThread.InvokeRequired) {
            if (_disposed || _ctx.Target.IsDisposed)
                return;
            try {
                _ctx.UiThread.BeginInvoke(new Action(() => {
                    if (_disposed || _ctx.Target.IsDisposed)
                        return;
                    Render();
                }));
            }
            catch (InvalidOperationException) { }
            return;
        }

        IsRendering = true;
        var sw = Stopwatch.StartNew();
        try {
            _ctx.Skin.ApplyLastState();
            Render();
        }
        finally {
            sw.Stop();
            LastRenderDurationMs = sw.ElapsedMilliseconds;
            IsRendering = false;
        }
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

    public void EnsureBuffers(int width, int height) {
        if (width <= 0 || height <= 0)
            return;

        if (_renderBuffer == null || _renderBuffer.Width != width || _renderBuffer.Height != height) {
            _renderSurface?.Dispose();
            _renderBuffer?.Dispose();
            _renderBuffer = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _renderSurface = SKSurface.Create(_renderBuffer.Info, _renderBuffer.GetPixels(), _renderBuffer.RowBytes);
        }

        if (_backBuffer == null || _backBuffer.Width != width || _backBuffer.Height != height) {
            _backBuffer?.Dispose();
            _backBuffer = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        }
    }

    public void Render() {
        try {
            if (_ctx.Target.IsDisposed || !_ctx.Target.IsHandleCreated || _disposed)
                return;

            GetDeviceSize(out int width, out int height);
            Resize(width, height);

            var canvas = _renderSurface!.Canvas;
            DrawWindowFrame(canvas, new Size(width, height));
            _ctx.Controls.Draw(canvas);

            // Fehlende Platzhalter zuletzt zeichnen -> topmost
            if (_missingRects.Count > 0) {
                foreach (var rect in _missingRects) {
                    DrawMissingImageError(canvas, rect);
                }
                _missingRects.Clear();
            }

            var bmpData = _backBuffer!.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppPArgb);

            try {
                unsafe {
                    byte* src = (byte*)_renderBuffer!.GetPixels();
                    if (src == null)
                        return;

                    int srcRowBytes = _renderBuffer.RowBytes;
                    long srcExpectedBytes = (long)srcRowBytes * height;
                    long skByteCount = _renderBuffer.ByteCount;
                    if (skByteCount < srcExpectedBytes)
                        return;

                    byte* dst = (byte*)bmpData.Scan0;
                    if (dst == null)
                        return;

                    int dstRowBytes = bmpData.Stride;
                    if (dstRowBytes > srcRowBytes) {
                        var pool = ArrayPool<byte>.Shared;
                        byte[] zeros = pool.Rent(dstRowBytes);
                        try {
                            Array.Clear(zeros, 0, dstRowBytes);
                            for (int y = 0; y < height; y++) {
                                IntPtr rowPtr = new((byte*)dst + (long)y * dstRowBytes);
                                Marshal.Copy(zeros, 0, rowPtr, dstRowBytes);
                            }
                        }
                        finally { pool.Return(zeros); }
                    }

                    long dstExpectedBytes = (long)dstRowBytes * height;
                    if (srcRowBytes <= 0 || dstRowBytes <= 0 || height <= 0)
                        return;
                    if (srcExpectedBytes > Int64.MaxValue / 2 || dstExpectedBytes > Int64.MaxValue / 2)
                        return;

                    if (srcRowBytes == dstRowBytes && skByteCount >= srcExpectedBytes && dstExpectedBytes <= skByteCount) {
                        Buffer.MemoryCopy(src, dst, dstExpectedBytes, srcExpectedBytes);
                    } else {
                        int bytesPerRowToCopy = Math.Min(srcRowBytes, dstRowBytes);
                        for (int y = 0; y < height; y++) {
                            byte* sRow = src + (long)y * srcRowBytes;
                            byte* dRow = dst + (long)y * dstRowBytes;
                            long sOffset = (long)y * srcRowBytes + bytesPerRowToCopy;
                            long dOffset = (long)y * dstRowBytes + bytesPerRowToCopy;
                            if (sOffset > skByteCount || dOffset > dstExpectedBytes)
                                break;
                            Buffer.MemoryCopy(sRow, dRow, dstRowBytes, bytesPerRowToCopy);
                        }
                    }
                }
            }
            finally { _backBuffer.UnlockBits(bmpData); }

            Present(_backBuffer);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Render error: {ex}");
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
        IntPtr hDib = IntPtr.Zero;
        IntPtr oldObj = IntPtr.Zero;
        IntPtr dibPixels = IntPtr.Zero;

        try {
            screenDc = NativeMethods.GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
                return;

            memDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memDc == IntPtr.Zero)
                return;

            int width = bitmap.Width;
            int height = bitmap.Height;

            var bmi = new NativeMethods.BITMAPINFO
            {
                bmiHeader = new NativeMethods.BITMAPINFOHEADER
                {
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

            hDib = NativeMethods.CreateDIBSection(screenDc, ref bmi, NativeMethods.DIB_RGB_COLORS, out dibPixels, IntPtr.Zero, 0);
            if (hDib == IntPtr.Zero || dibPixels == IntPtr.Zero)
                return;

            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppPArgb);

            try {
                unsafe {
                    byte* src = (byte*)bmpData.Scan0;
                    byte* dst = (byte*)dibPixels;
                    int srcStride = bmpData.Stride;
                    int dstStride = width * 4;

                    if (srcStride == dstStride) {
                        Buffer.MemoryCopy(src, dst, (long)dstStride * height, (long)srcStride * height);
                    } else {
                        for (int y = 0; y < height; y++) {
                            byte* sRow = src + (long)y * srcStride;
                            byte* dRow = dst + (long)y * dstStride;
                            int bytesToCopy = Math.Min(srcStride, dstStride);
                            Buffer.MemoryCopy(sRow, dRow, dstStride, bytesToCopy);
                        }
                    }
                }
            }
            finally { bitmap.UnlockBits(bmpData); }

            oldObj = NativeMethods.SelectObject(memDc, hDib);

            Size size = new(width, height);
            Point source = new(0, 0);
            Point topPos = new(_ctx.Target.Left, _ctx.Target.Top);

            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = opacity,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            bool success = NativeMethods.UpdateLayeredWindow(
                hwnd, screenDc, ref topPos, ref size, memDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);

            if (!success) {
                var err = Marshal.GetLastWin32Error();
                LastPresentError = err;
                Debug.WriteLine($"UpdateLayeredWindow failed: {err}");
            }
        }
        finally {
            if (memDc != IntPtr.Zero)
                _ = NativeMethods.SelectObject(memDc, oldObj);
            if (hDib != IntPtr.Zero)
                _ = NativeMethods.DeleteObject(hDib);
            if (memDc != IntPtr.Zero)
                _ = NativeMethods.DeleteDC(memDc);
            if (screenDc != IntPtr.Zero)
                _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    // Zeichnet ein rotes X über schwarzem Hintergrund in das gegebene Rect
    // Zusätzlich zentriert das Wort "MISSING IMAGE" entlang der längeren Kante (horizontal zur längeren Kante)
    private static void DrawMissingImageError(SKCanvas canvas, SKRect rect) {
        using var bgPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Black, IsAntialias = true };
        using var borderPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Magenta, StrokeWidth = 2, IsAntialias = true };
        using var xPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Black, IsAntialias = true, StrokeCap = SKStrokeCap.Square };
        using var textPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Magenta, IsAntialias = true };

        float stroke = Math.Max(4f, Math.Min(rect.Width, rect.Height) / 8f);
        xPaint.StrokeWidth = stroke;

        canvas.DrawRect(rect, bgPaint);
        canvas.DrawRect(rect, borderPaint);

        //float inset = stroke / 2f + 1f;

        //var p1 = new SKPoint(rect.Left + inset, rect.Top + inset);
        //var p2 = new SKPoint(rect.Right - inset, rect.Bottom - inset);
        //var p3 = new SKPoint(rect.Left + inset, rect.Bottom - inset);
        //var p4 = new SKPoint(rect.Right - inset, rect.Top + inset);

        //canvas.DrawLine(p1, p2, xPaint);
        //canvas.DrawLine(p3, p4, xPaint);

        // Text "MISSING" entlang der längeren Kante (immer horizontal zur längeren Kante)
        float longSide = Math.Max(rect.Width, rect.Height);
        float fontSize = Math.Max(10f, longSide / 20f);
        using var font = new SKFont(SKTypeface.Default, fontSize);
        var metrics = font.Metrics;

        // Zeichnen zentriert; bei Bedarf rotieren, damit Text horizontal zur längeren Kante liegt
        float cx = rect.MidX;
        float cy = rect.MidY;
        bool rotate = rect.Height > rect.Width; // wenn Hochformat, rotiere -90deg damit Text "horizontal" zur längeren Kante erscheint

        canvas.Save();
        canvas.Translate(cx, cy);
        if (rotate) {
            canvas.RotateDegrees(-90);
        }

        // Nach Translate ist Zentrum (0,0). Berechne baseline y so dass Text vertikal zentriert bleibt.
        float baselineY = -(metrics.Descent + metrics.Ascent) / 2f;

        canvas.DrawText("MISSING IMAGE", 0, baselineY, SKTextAlign.Center, font, textPaint);
        canvas.Restore();
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

        // Ecken
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

        // Top Center
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

        // Bottom Center
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

        // Left Center
        if (bg.LeftCenter != null) {
            float top = (bg.TopLeft?.Height) ?? 0f;
            float bottom = top + (height - ((bg.TopLeft?.Height) ?? 0f) - ((bg.BottomLeft?.Height) ?? 0f) + layout.LeftHeightOffset);
            if (bottom > top)
                canvas.DrawBitmap(bg.LeftCenter, new SKRect(0, top, bg.LeftCenter.Width, bottom));
        } else {
            float top = (bg.TopLeft?.Height) ?? 0f;
            float bottom = top + (height - ((bg.TopLeft?.Height) ?? 0f) - ((bg.BottomLeft?.Height) ?? 0f) + layout.LeftHeightOffset);
            if (bottom > top)
                _missingRects.Add(new SKRect(0, top, Math.Min(64, width / 6f), bottom));
        }

        // Right Center
        if (bg.RightCenter != null) {
            float left = width - bg.RightCenter.Width;
            float top = (bg.TopRight?.Height) ?? 0f;
            float bottom = top + (height - ((bg.TopRight?.Height) ?? 0f) - ((bg.BottomRight?.Height) ?? 0f) + layout.RightHeightOffset);
            if (bottom > top)
                canvas.DrawBitmap(bg.RightCenter, new SKRect(left, top, left + bg.RightCenter.Width, bottom));
        } else {
            float left = width - Math.Min(64, width / 6f);
            float top = (bg.TopRight?.Height) ?? 0f;
            float bottom = top + (height - ((bg.TopRight?.Height) ?? 0f) - ((bg.BottomRight?.Height) ?? 0f) + layout.RightHeightOffset);
            if (bottom > top)
                _missingRects.Add(new SKRect(left, top, width, bottom));
        }

        // Fill-Bereich: sichere Berechnung mit Fallbacks und Validierung
        _fillPaint.Color = bg.FillColor.ToSKColor();
        float leftWidth = (bg.LeftCenter?.Width) ?? 0f;
        float topHeight = (bg.TopCenter?.Height) ?? 0f;
        float bottomHeight = (bg.BottomCenter?.Height) ?? 0f;
        float fillLeft = Math.Max(0f, leftWidth - layout.FillPosOffset);
        float fillTop = Math.Max(0f, topHeight - layout.FillPosOffset);
        float fillRight = fillLeft + Math.Max(0f, width - leftWidth * 2 + layout.FillWidthOffset);
        float fillBottom = fillTop + Math.Max(0f, height - topHeight - bottomHeight + layout.FillHeightOffset);
        if (fillRight > fillLeft && fillBottom > fillTop) {
            canvas.DrawRect(new SKRect(fillLeft, fillTop, fillRight, fillBottom), _fillPaint);
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        // Reset Render-Anforderung unabhängig vom disposing-Zustand
        _needsRender = 0;

        if (disposing) {
            try {
                // Event abmelden bevor der Timer disposed wird
                _renderTimer.Tick -= RenderTimer_Tick;
            }
            catch { }

            try { _renderTimer.Stop(); }
            catch { }

            try { _renderTimer.Dispose(); }
            catch { }

            try { _renderSurface?.Dispose(); }
            catch { }

            try { _renderBuffer?.Dispose(); }
            catch { }

            try { _backBuffer?.Dispose(); }
            catch { }

            try { _fillPaint.Dispose(); }
            catch { }
        }

        // Hier könnten unverwaltete Ressourcen freigegeben werden, falls später hinzugefügt.
        // Felder auf "leeren" Zustand setzen (readonly Felder bleiben unverändert)
        _renderSurface = null;
        _renderBuffer = null;
        _backBuffer = null;
        _disposed = true;
    }
}
