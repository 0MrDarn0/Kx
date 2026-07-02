// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using SkiaSharp;

namespace Kx.UI.Rendering;

internal sealed class PerformanceOverlay : IRenderOverlay, IDisposable {
    private readonly object _perfLock = new();
    private readonly Queue<long> _frameTimestamps = new();
    private readonly object _cpuLock = new();

    private Process? _cachedProcess;
    private SKPaint? _perfBgPaint;
    private SKPaint? _perfTextPaint;
    private SKPaint? _perfTitlePaint;
    private SKFont? _perfMonoFont;
    private SKPaint? _barBgPaint;
    private SKPaint? _barFillPaint;

    private TimeSpan _lastTotalProcTime = TimeSpan.Zero;
    private long _lastCpuSampleMs;
    private long _lastOverlayUpdateMs;
    private double _cachedCpuPercent;
    private long _cachedManagedKb;
    private int _cachedHandleCount;
    private int _cachedThreadCount;
    private string _cachedUptime = "—";

    public const string OverlayId = "performance";
    private const int FrameHistory = 60;
    private const int OverlayUpdateIntervalMs = 200;

    public string Id => OverlayId;

    public void Draw(RenderOverlayContext context) {
        ArgumentNullException.ThrowIfNull(context);

        RecordFrameTimestamp(context.CurrentTimestampMs);

        SKCanvas canvas = context.Canvas;
        long nowMs = context.CurrentTimestampMs;

        _cachedProcess ??= TryGetCurrentProcess();
        _perfBgPaint ??= new SKPaint { Color = new SKColor(0, 0, 0, 160), IsAntialias = true, Style = SKPaintStyle.Fill };
        _perfTextPaint ??= new SKPaint { Color = SKColors.Lime, IsAntialias = true };
        _perfTitlePaint ??= new SKPaint { Color = SKColors.White, IsAntialias = true };
        _perfMonoFont ??= new SKFont(SKTypeface.FromFamilyName("Consolas"), 12);
        _barBgPaint ??= new SKPaint { Color = new SKColor(255, 255, 255, 30), IsAntialias = true };
        _barFillPaint ??= new SKPaint { Color = SKColors.Lime, IsAntialias = true };

        if (nowMs - _lastOverlayUpdateMs >= OverlayUpdateIntervalMs) {
            _lastOverlayUpdateMs = nowMs;
            UpdateCachedMemoryAndProcessMetrics();
            UpdateCpuMetrics(nowMs);
        }

        double fps = CalculateFps();
        long lastRenderMs = context.LastRenderDurationMs;

        var preLines = new (string Label, string Value)[] {
            ("FPS", fps > 0 ? $"{fps:F1}" : "—"),
            ("Render (last)", $"{lastRenderMs} ms"),
            ("CPU", $"{_cachedCpuPercent:F1} %"),
            ("Managed", $"{_cachedManagedKb:N0} KB"),
            ("WorkingSet", $"{Process.GetCurrentProcess().WorkingSet64 / 1024:N0} KB"),
            ("GC (0/1/2)", $"{GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}"),
            ("Handles", _cachedHandleCount.ToString()),
            ("Threads", _cachedThreadCount.ToString()),
            ("Buffer", $"{context.BufferWidth}x{context.BufferHeight}")
        };

        float padding = 12f;
        float lineHeight = 16f;
        float boxWidth = 320f;
        float barHeight = 8f;

        int totalSlots = preLines.Length + 1 + 1;
        float boxHeight = padding * 2 + lineHeight * totalSlots;
        var rect = new SKRect(padding, padding, padding + boxWidth, padding + boxHeight);

        canvas.DrawRoundRect(rect, 6, 6, _perfBgPaint);

        float x = rect.Left + padding;
        float y = rect.Top + padding + lineHeight;

        void DrawLine(string label, string value) {
            canvas.DrawText(label, x, y, SKTextAlign.Left, _perfMonoFont, _perfTitlePaint);
            canvas.DrawText(value, x + 150, y, SKTextAlign.Left, _perfMonoFont, _perfTextPaint);
            y += lineHeight;
        }

        foreach (var (label, value) in preLines)
            DrawLine(label, value);

        DrawLine("Uptime", _cachedUptime);

        float barX = x;
        float barWidth = boxWidth - 2 * padding;
        float barY = y + (lineHeight - barHeight) / 2f;

        float normalized = Math.Min(1f, lastRenderMs / 50f);
        _barFillPaint.Color = normalized < 0.5 ? SKColors.Lime : SKColors.OrangeRed;

        canvas.DrawRect(barX, barY, barWidth, barHeight, _barBgPaint);
        canvas.DrawRect(barX, barY, barWidth * normalized, barHeight, _barFillPaint);
    }

    public void Dispose() {
        _perfBgPaint?.Dispose();
        _perfTextPaint?.Dispose();
        _perfTitlePaint?.Dispose();
        _perfMonoFont?.Dispose();
        _barBgPaint?.Dispose();
        _barFillPaint?.Dispose();
        _cachedProcess?.Dispose();
    }

    private void RecordFrameTimestamp(long now) {
        lock (_perfLock) {
            _frameTimestamps.Enqueue(now);
            while (_frameTimestamps.Count > FrameHistory)
                _frameTimestamps.Dequeue();
        }
    }

    private double CalculateFps() {
        lock (_perfLock) {
            int frameCount = _frameTimestamps.Count;
            if (frameCount < 2)
                return 0;

            long first = _frameTimestamps.Peek();
            long last = _frameTimestamps.Last();
            double span = Math.Max(1, last - first);
            return (frameCount - 1) * 1000.0 / span;
        }
    }

    private void UpdateCachedMemoryAndProcessMetrics() {
        try {
            _cachedManagedKb = GC.GetTotalMemory(false) / 1024;

            if (_cachedProcess is null)
                return;

            try {
                _cachedHandleCount = _cachedProcess.HandleCount;
                _cachedThreadCount = _cachedProcess.Threads.Count;
                var uptime = DateTime.Now - _cachedProcess.StartTime;
                _cachedUptime = uptime.TotalSeconds >= 1 ? $"{uptime:hh\\:mm\\:ss}" : "—";
            }
            catch {
                _cachedHandleCount = 0;
                _cachedThreadCount = 0;
                _cachedUptime = "—";
            }
        }
        catch {
        }
    }

    private void UpdateCpuMetrics(long nowMs) {
        try {
            lock (_cpuLock) {
                if (_cachedProcess is null)
                    return;

                var totalProc = _cachedProcess.TotalProcessorTime;
                if (_lastCpuSampleMs != 0) {
                    double deltaProcMs = (totalProc - _lastTotalProcTime).TotalMilliseconds;
                    double deltaWallMs = Math.Max(1.0, nowMs - _lastCpuSampleMs);
                    _cachedCpuPercent = (deltaProcMs / deltaWallMs) * 100.0 / Math.Max(1, Environment.ProcessorCount);
                    _cachedCpuPercent = Math.Max(0.0, Math.Min(100.0, _cachedCpuPercent));
                }

                _lastTotalProcTime = totalProc;
                _lastCpuSampleMs = nowMs;
            }
        }
        catch {
            _cachedCpuPercent = 0.0;
        }
    }

    private static Process? TryGetCurrentProcess() {
        try {
            return Process.GetCurrentProcess();
        }
        catch {
            return null;
        }
    }
}
