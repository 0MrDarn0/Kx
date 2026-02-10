// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using KUpdater.Interop;

namespace KUpdater.Utility;

public sealed class HotkeyEventArgs : EventArgs {
    public int Id { get; }
    public Keys Key { get; }
    public uint Modifiers { get; }

    public HotkeyEventArgs(int id, Keys key, uint modifiers) {
        Id = id;
        Key = key;
        Modifiers = modifiers;
    }
}

public sealed class HotkeyManager : IDisposable {
    // Internes Mapping id -> (key, modifiers)
    private readonly ConcurrentDictionary<int, (Keys key, uint modifiers)> _hotkeys = new();
    private int _nextId = 0;
    private readonly IntPtr _windowHandle;
    private readonly SynchronizationContext? _syncContext;

    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public HotkeyManager(IntPtr windowHandle) {
        _windowHandle = windowHandle;
        _syncContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Registriert einen globalen Hotkey für das gegebene Window handle.
    /// Liefert die interne id zurück, die zum Unregister verwendet werden kann.
    /// </summary>
    public int Register(uint modifiers, Keys key) {
        int id = Interlocked.Increment(ref _nextId);
        bool ok = NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, (uint)key);
        if (!ok) {
            int err = Marshal.GetLastWin32Error();
            Debug.WriteLine($"RegisterHotKey failed (id={id}, key={key}, mod={modifiers}): {err}");
            throw new InvalidOperationException($"RegisterHotKey failed: {err}");
        }

        _hotkeys[id] = (key, modifiers);
        return id;
    }

    public bool Unregister(int id) {
        if (!_hotkeys.TryRemove(id, out _))
            return false;

        bool ok = NativeMethods.UnregisterHotKey(_windowHandle, id);
        if (!ok) {
            int err = Marshal.GetLastWin32Error();
            Debug.WriteLine($"UnregisterHotKey failed (id={id}): {err}");
        }
        return ok;
    }

    /// <summary>
    /// Muss aus der Form.WndProc aufgerufen werden. Liefert true wenn die Message verarbeitet wurde.
    /// </summary>
    public bool ProcessWndProc(ref Message m) {
        if (m.Msg != NativeMethods.WM_HOTKEY)
            return false;

        int id = m.WParam.ToInt32();
        if (_hotkeys.TryGetValue(id, out var info)) {
            var args = new HotkeyEventArgs(id, info.key, info.modifiers);
            // Marshal to original sync context (UI thread) falls vorhanden
            if (_syncContext != null)
                _syncContext.Post(_ => HotkeyPressed?.Invoke(this, args), null);
            else
                HotkeyPressed?.Invoke(this, args);
            return true;
        }

        return false;
    }

    public void UnregisterAll() {
        foreach (var id in _hotkeys.Keys) {
            try { Unregister(id); }
            catch { }
        }
        _hotkeys.Clear();
    }

    public void Dispose() {
        UnregisterAll();
        GC.SuppressFinalize(this);
    }
}
