// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

namespace KUpdater.Core;

public sealed class CachedWindowParams {
    public int DeviceWidth { get; }
    public int DeviceHeight { get; }
    public int Left { get; }
    public int Top { get; }
    public int DeviceDpi { get; }
    public long TimestampMs { get; }

    public CachedWindowParams(int deviceWidth, int deviceHeight, int left, int top, int deviceDpi) {
        DeviceWidth = deviceWidth;
        DeviceHeight = deviceHeight;
        Left = left;
        Top = top;
        DeviceDpi = deviceDpi;
        TimestampMs = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
    }
}
