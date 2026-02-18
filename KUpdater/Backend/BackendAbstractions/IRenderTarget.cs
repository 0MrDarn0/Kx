// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Backend.BackendAbstractions;

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
