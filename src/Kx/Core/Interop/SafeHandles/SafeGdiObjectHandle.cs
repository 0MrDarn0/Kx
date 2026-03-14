// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Microsoft.Win32.SafeHandles;

namespace Kx.Core.Interop.SafeHandles;

internal sealed class SafeGdiObjectHandle : SafeHandleZeroOrMinusOneIsInvalid {
    public SafeGdiObjectHandle() : base(true) { }
    public SafeGdiObjectHandle(IntPtr hObj) : base(true) {
        SetHandle(hObj);
    }
    public void Attach(IntPtr hObj) => SetHandle(hObj);
    protected override bool ReleaseHandle() => NativeMethods.DeleteObject(handle);
}
