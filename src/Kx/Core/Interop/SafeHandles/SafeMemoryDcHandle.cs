// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Microsoft.Win32.SafeHandles;

namespace Kx.Core.Interop.SafeHandles;


internal sealed class SafeMemoryDcHandle : SafeHandleZeroOrMinusOneIsInvalid {
    public SafeMemoryDcHandle() : base(true) { }
    public SafeMemoryDcHandle(IntPtr hdc) : base(true) {
        SetHandle(hdc);
    }
    public void Attach(IntPtr hdc) => SetHandle(hdc);
    protected override bool ReleaseHandle() => NativeMethods.DeleteDC(handle);
}
