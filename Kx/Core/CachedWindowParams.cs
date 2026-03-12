// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

namespace Kx.Core;

public sealed class CachedWindowParams(int deviceWidth, int deviceHeight, int left, int top, int deviceDpi) {
    public int DeviceWidth { get; } = deviceWidth;
    public int DeviceHeight { get; } = deviceHeight;
    public int Left { get; } = left;
    public int Top { get; } = top;
    public int DeviceDpi { get; } = deviceDpi;
    public long TimestampMs { get; } = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
}
