// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.App;
using Kx.UI.Themes;

using SkiaSharp;

namespace Kx.UI.Rendering;

internal sealed class RenderOverlayContext {
    public RenderOverlayContext(
        WindowContext windowContext,
        FrameResource? frame,
        SKCanvas canvas,
        Size size,
        long lastRenderDurationMs,
        int bufferWidth,
        int bufferHeight,
        Action requestRender) {
        ArgumentNullException.ThrowIfNull(windowContext);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(requestRender);

        WindowContext = windowContext;
        Frame = frame;
        Canvas = canvas;
        Size = size;
        LastRenderDurationMs = lastRenderDurationMs;
        BufferWidth = bufferWidth;
        BufferHeight = bufferHeight;
        RequestRender = requestRender;
    }

    public WindowContext WindowContext { get; }
    public FrameResource? Frame { get; }
    public SKCanvas Canvas { get; }
    public Size Size { get; }
    public long LastRenderDurationMs { get; }
    public int BufferWidth { get; }
    public int BufferHeight { get; }
    public Action RequestRender { get; }

    public float DeviceScale => Math.Max(1f, WindowContext.Target.DeviceDpi / 96f);

    public SKRect ContentRect => Frame?.GetContentRect(Size) ?? new SKRect(0, 0, Size.Width, Size.Height);

    public long CurrentTimestampMs => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
}
