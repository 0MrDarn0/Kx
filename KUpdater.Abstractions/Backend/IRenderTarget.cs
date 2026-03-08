// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Backend;

public interface IRenderTarget {
    IntPtr Handle { get; }
    bool IsDisposed { get; }
    bool IsHandleCreated { get; }

    int Left { get; }
    int Top { get; }
    int Width { get; }
    int Height { get; }
    int DeviceDpi { get; }
}
