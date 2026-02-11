// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Interop;
using KUpdater.UI;

namespace KUpdater;

internal static class Program {
    // Unique name for the mutex — use a GUID or app‑specific ID
    private static readonly string AppMutexName = "Global\\{C0A76B5A-12AB-45C5-B9D9-D693FAA6E7B9}";
    private static Mutex? Mutex;

    [STAThread]
    static void Main() {

        Mutex = new Mutex(initiallyOwned: true, name: AppMutexName, createdNew: out bool createdNew);
        if (!createdNew) {
            BringExistingInstanceToFront();
            return;
        }

        var backend = new WinFormsBackend();
        var window = new Window(backend);
        backend.Shown += (_, _) => window.OnShown();
        backend.FormClosed += (_, e) => window.OnClosed(e.CloseReason == CloseReason.UserClosing);
        Application.Run(backend);
        GC.KeepAlive(Mutex);
    }

    private static void BringExistingInstanceToFront() {
        try {
            Process current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName)) {
                if (process.Id != current.Id) {
                    IntPtr hWnd = process.MainWindowHandle;
                    if (hWnd != IntPtr.Zero) {
                        // If minimized, restore first
                        if (NativeMethods.IsIconic(hWnd)) {
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                        }

                        // Then bring to front
                        NativeMethods.SetForegroundWindow(hWnd);
                    }
                    break;
                }
            }
        }
        catch {
            // Ignore errors silently
        }
    }
}
