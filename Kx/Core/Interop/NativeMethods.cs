// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Runtime.InteropServices;

namespace Kx.Core.Interop;

[Flags]
public enum WindowStylesEx : int {
    WS_EX_DLGMODALFRAME       = 0x00000001,
    WS_EX_NOPARENTNOTIFY      = 0x00000004,
    WS_EX_TOPMOST             = 0x00000008,
    WS_EX_ACCEPTFILES         = 0x00000010,
    WS_EX_TRANSPARENT         = 0x00000020,
    WS_EX_MDICHILD            = 0x00000040,
    WS_EX_TOOLWINDOW          = 0x00000080,
    WS_EX_WINDOWEDGE          = 0x00000100,
    WS_EX_CLIENTEDGE          = 0x00000200,
    WS_EX_CONTEXTHELP         = 0x00000400,

    WS_EX_RIGHT               = 0x00001000,
    WS_EX_LEFT                = 0x00000000,
    WS_EX_RTLREADING          = 0x00002000,
    WS_EX_LTRREADING          = 0x00000000,
    WS_EX_LEFTSCROLLBAR       = 0x00004000,
    WS_EX_RIGHTSCROLLBAR      = 0x00000000,

    WS_EX_CONTROLPARENT       = 0x00010000,
    WS_EX_STATICEDGE          = 0x00020000,
    WS_EX_APPWINDOW           = 0x00040000,

    WS_EX_OVERLAPPEDWINDOW    = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
    WS_EX_PALETTEWINDOW       = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,

    WS_EX_LAYERED             = 0x00080000,
    WS_EX_NOINHERITLAYOUT     = 0x00100000,
    WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
    WS_EX_LAYOUTRTL           = 0x00400000,
    WS_EX_COMPOSITED          = 0x02000000,
    WS_EX_NOACTIVATE          = 0x08000000
}

[Flags]
public enum RedrawWindowFlags : uint {
    Invalidate   = 0x0001,
    AllChildren  = 0x0080,
    UpdateNow    = 0x0100,
    Frame        = 0x0400,
}

[Flags]
public enum HotKeyModifiers : uint {
    None      = 0x0000,
    Alt       = 0x0001,
    Control   = 0x0002,
    Shift     = 0x0004,
    Win       = 0x0008,
    NoRepeat  = 0x4000,
}

internal static class NativeMethods {
    // --------------------------------------------------------------------
    // Constants
    // --------------------------------------------------------------------
    public const int ULW_ALPHA = 0x00000002;
    public const byte AC_SRC_OVER = 0x00;
    public const byte AC_SRC_ALPHA = 0x01;

    public const int SW_RESTORE = 9;

    public const uint RDW_INVALIDATE = 0x0001;
    public const uint RDW_UPDATENOW = 0x0100;
    public const uint RDW_FRAME = 0x0400;
    public const uint RDW_ALLCHILDREN = 0x0080;

    public const int WM_HOTKEY = 0x0312;

    public const uint MOD_NONE = 0x0000;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public const uint BI_RGB = 0u;
    public const int DIB_RGB_COLORS = 0;

    public const int WM_SETICON = 0x0080;
    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int GCL_HICON = -14;
    public const int GCL_HICONSM = -34;

    public const uint PAGE_READWRITE = 0x04;
    public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    // --------------------------------------------------------------------
    // Structs
    // --------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO {
        public BITMAPINFOHEADER bmiHeader;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] bmiColors;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    // --------------------------------------------------------------------
    // SetClassLongPtr (x86/x64 sicher)
    // --------------------------------------------------------------------

    // x64 → SetClassLongPtrW
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetClassLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // x86 → SetClassLongW
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetClassLongW(IntPtr hWnd, int nIndex, uint dwNewLong);

    public static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
        if (IntPtr.Size == 8) {
            return SetClassLongPtrW(hWnd, nIndex, dwNewLong);
        } else {
            uint result = SetClassLongW(hWnd, nIndex, unchecked((uint)dwNewLong.ToInt32()));
            return new IntPtr(unchecked((int)result));
        }
    }

    // --------------------------------------------------------------------
    // Kernel32
    // --------------------------------------------------------------------
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFileMapping(
        IntPtr hFile,
        IntPtr lpFileMappingAttributes,
        uint flProtect,
        uint dwMaximumSizeHigh,
        uint dwMaximumSizeLow,
        string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    // --------------------------------------------------------------------
    // GDI32
    // --------------------------------------------------------------------
    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateDIBSection(
        IntPtr hdc,
        ref BITMAPINFO pbmi,
        uint iUsage,
        out IntPtr ppvBits,
        IntPtr hSection,
        uint dwOffset);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteDC(IntPtr hdc);

    // --------------------------------------------------------------------
    // User32
    // --------------------------------------------------------------------
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UpdateLayeredWindow(
        IntPtr hwnd,
        IntPtr hdcDst,
        ref Point pptDst,
        ref Size psize,
        IntPtr hdcSrc,
        ref Point pptSrc,
        int crKey,
        ref BLENDFUNCTION pblend,
        int dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RedrawWindow(
        IntPtr hWnd,
        IntPtr lprcUpdate,
        IntPtr hrgnUpdate,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
}
