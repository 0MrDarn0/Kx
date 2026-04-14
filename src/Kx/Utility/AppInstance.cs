// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using Kx.Core.Interop;

namespace Kx.Utility;

/// <summary>
/// Wrapper around a named Mutex to ensure single instance and provide a BringToFront helper.
/// </summary>
public sealed class AppInstance : IDisposable {
    private readonly Mutex _mutex;
    private bool _ownsMutex;
    public string Name { get; }

    private AppInstance(string name, Mutex mutex, bool ownsMutex) {
        Name = name;
        _mutex = mutex;
        _ownsMutex = ownsMutex;
    }

    /// <summary>
    /// Try to acquire a named mutex. Returns null if another instance already runs.
    /// Caller must Dispose() the returned AppInstance when shutting down.
    /// </summary>
    public static AppInstance? Acquire(string name) {
        // initiallyOwned: true attempts to take ownership immediately
        var mutex = new Mutex(initiallyOwned: true, name: name, createdNew: out bool createdNew);
        if (!createdNew) {
            // We did not create it -> another instance exists
            // Close the local handle and return null
            mutex.Dispose();
            return null;
        }

        return new AppInstance(name, mutex, ownsMutex: true);
    }

    /// <summary>
    /// Bring an existing instance's main window to front (best effort).
    /// Returns true if a window was found and brought to front, false otherwise.
    /// </summary>
    public static bool BringExistingInstanceToFront(string processName) {
        bool foundWindow = false;
        try {
            var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(processName)) {
                if (process.Id == current.Id)
                    continue;
                var hWnd = process.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                    continue;

                if (NativeMethods.IsIconic(hWnd))
                    NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);

                NativeMethods.SetForegroundWindow(hWnd);
                foundWindow = true;
                break;
            }
        }
        catch {
            // best-effort: swallow exceptions to avoid crashing the caller
        }
        return foundWindow;
    }

    /// <summary>
    /// Checks if there are any processes with the given name that have no main window (potential zombie processes).
    /// Returns the process IDs of such processes.
    /// </summary>
    public static List<int> FindZombieProcesses(string processName) {
        var zombies = new List<int>();
        try {
            var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(processName)) {
                if (process.Id == current.Id)
                    continue;

                // A process without a main window is potentially a zombie
                if (process.MainWindowHandle == IntPtr.Zero) {
                    zombies.Add(process.Id);
                }
            }
        }
        catch {
            // best-effort: swallow exceptions
        }
        return zombies;
    }

    public void Dispose() {
        if (_ownsMutex) {
            try { _mutex.ReleaseMutex(); }
            catch { /* ignore */ }
        }
        _mutex.Dispose();
        _ownsMutex = false;
    }
}

