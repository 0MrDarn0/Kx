// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.Backend;
using KUpdater.Abstractions.Events;
using KUpdater.Core.Interop;
using KUpdater.Core.Localization;
using KUpdater.Utility;

namespace KUpdater.Backend.WinForms;

/// <summary>
/// WinForms‑Implementierung des Window‑Backends.
/// Liefert plattform­spezifische Events und Operationen für die UI‑unabhängige Abstraktion.
/// </summary>
public class WinFormsBackend : Form, IRenderTarget, IUiThreadInvoker, IWindowBackend {
    // IRenderTarget (explizit implementiert)

    /// <summary>Handle des Win32‑Fensters.</summary>
    IntPtr IRenderTarget.Handle => Handle;

    /// <summary>Gibt an, ob das Control bereits disposed wurde.</summary>
    bool IRenderTarget.IsDisposed => IsDisposed;

    /// <summary>Gibt an, ob das Fensterhandle erstellt wurde.</summary>
    bool IRenderTarget.IsHandleCreated => IsHandleCreated;

    /// <summary>Linke Position des Fensters.</summary>
    int IRenderTarget.Left => Left;

    /// <summary>Obere Position des Fensters.</summary>
    int IRenderTarget.Top => Top;

    /// <summary>Breite des Fensters.</summary>
    int IRenderTarget.Width => Width;

    /// <summary>Höhe des Fensters.</summary>
    int IRenderTarget.Height => Height;

    /// <summary>Aktuelle DPI des Geräts.</summary>
    int IRenderTarget.DeviceDpi => DeviceDpi;

    // IUiThreadInvoker (explizit implementiert)

    /// <summary>Gibt an, ob Aufrufe auf dem UI‑Thread erforderlich sind.</summary>
    bool IUiThreadInvoker.InvokeRequired => base.InvokeRequired;

    /// <summary>Beginnt einen asynchronen Aufruf auf dem UI‑Thread.</summary>
    void IUiThreadInvoker.BeginInvoke(Delegate d) => base.BeginInvoke(d);

    /// <summary>Führt eine Aktion synchron auf dem UI‑Thread aus.</summary>
    void IUiThreadInvoker.Invoke(Action action) => base.Invoke(action);

    private Icon? _appIcon;
    private bool _iconMissingNotified;

    // Backing‑Events für die Abstractions‑Events

    /// <summary>Backing‑Event für Größenänderungen.</summary>
    private event Action<WindowResizeEvent>? _resized;

    /// <summary>Backing‑Event für Mausbewegungen.</summary>
    private event Action<WindowMouseEvent>? _mouseMove;

    /// <summary>Backing‑Event für Maustasten gedrückt.</summary>
    private event Action<WindowMouseEvent>? _mouseDown;

    /// <summary>Backing‑Event für Maustasten losgelassen.</summary>
    private event Action<WindowMouseEvent>? _mouseUp;

    /// <summary>Backing‑Event für Mausradbewegungen.</summary>
    private event Action<WindowMouseEvent>? _mouseWheel;

    // Explizite Implementierung der Abstractions‑Events (vermeidet Namenskonflikte)

    /// <summary>Abstractions‑Event: Fenstergröße geändert.</summary>
    event Action<WindowResizeEvent>? KUpdater.Abstractions.Backend.IWindowBackend.Resized {
        add => _resized += value;
        remove => _resized -= value;
    }

    /// <summary>Abstractions‑Event: Maus bewegt.</summary>
    event Action<WindowMouseEvent>? KUpdater.Abstractions.Backend.IWindowBackend.MouseMove {
        add => _mouseMove += value;
        remove => _mouseMove -= value;
    }

    /// <summary>Abstractions‑Event: Maustaste gedrückt.</summary>
    event Action<WindowMouseEvent>? KUpdater.Abstractions.Backend.IWindowBackend.MouseDown {
        add => _mouseDown += value;
        remove => _mouseDown -= value;
    }

    /// <summary>Abstractions‑Event: Maustaste losgelassen.</summary>
    event Action<WindowMouseEvent>? KUpdater.Abstractions.Backend.IWindowBackend.MouseUp {
        add => _mouseUp += value;
        remove => _mouseUp -= value;
    }

    /// <summary>Abstractions‑Event: Mausrad bewegt.</summary>
    event Action<WindowMouseEvent>? KUpdater.Abstractions.Backend.IWindowBackend.MouseWheel {
        add => _mouseWheel += value;
        remove => _mouseWheel -= value;
    }

    /// <summary>Setzt die Fenstergröße (Client‑Bereich).</summary>
    public void SetSize(int width, int height) => Size = new Size(width, height);

    /// <summary>Setzt die Fensterposition (Bildschirmkoordinaten).</summary>
    public void SetPosition(int x, int y) => Location = new Point(x, y);

    /// <summary>Zeigt das Fenster an.</summary>
    public void ShowWindow() => Show();

    /// <summary>Schließt das Fenster.</summary>
    public void CloseWindow() => Close();

    // Explizite Implementierung für IWindowBackend.Cursor (Typkonflikt mit Control.Cursor vermeiden)

    /// <summary>
    /// Opaques Cursor‑Token für die Abstraktion.
    /// Intern wird ein WinForms Cursor erwartet und gesetzt.
    /// </summary>
    object? KUpdater.Abstractions.Backend.IWindowBackend.Cursor {
        get => base.Cursor;
        set {
            if (value is Cursor c)
                base.Cursor = c;
            else
                base.Cursor = value as Cursor;
        }
    }

    /// <summary>Initialisiert das Backend mit sinnvollen Standardwerten.</summary>
    public WinFormsBackend() {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        Width = 950;
        Height = 600;
    }

    /// <summary>Ergänzt Fenster‑CreateParams (z. B. Layered Window).</summary>
    protected override CreateParams CreateParams {
        get {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
            return cp;
        }
    }

    /// <summary>Verarbeitet Window‑Messages</summary>
    protected override void WndProc(ref Message m) {
        base.WndProc(ref m);
    }

    /// <summary>Lädt das App‑Icon beim Erstellen des Handles.</summary>
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        try {
            var path = Paths.GetResource("Default\\app.ico");
            if (File.Exists(path)) {
                _appIcon = new Icon(path);
                this.Icon = _appIcon;
                NativeMethods.SendMessage(this.Handle, NativeMethods.WM_SETICON, new IntPtr(NativeMethods.ICON_BIG), _appIcon.Handle);
                NativeMethods.SendMessage(this.Handle, NativeMethods.WM_SETICON, new IntPtr(NativeMethods.ICON_SMALL), _appIcon.Handle);
                NativeMethods.SetClassLongPtr(this.Handle, NativeMethods.GCL_HICON, _appIcon.Handle);
                NativeMethods.SetClassLongPtr(this.Handle, NativeMethods.GCL_HICONSM, _appIcon.Handle);
            } else {
                if (!_iconMissingNotified) {
                    _iconMissingNotified = true;
                    MessageBox.Show(this,
                        LanguageService.Translate("dialog.app_icon_missing.message", path),
                        LanguageService.Translate("dialog.app_icon_missing.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"Fehler beim Laden des App-Icons: {ex}");
        }
    }

    /// <summary>Feuert das Resized‑Event der Abstraktion.</summary>
    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        _resized?.Invoke(new WindowResizeEvent(Width, Height));
    }

    /// <summary>Feuert das MouseMove‑Event der Abstraktion.</summary>
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        _mouseMove?.Invoke(MapMouse(e));
    }

    /// <summary>Feuert das MouseDown‑Event der Abstraktion.</summary>
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        _mouseDown?.Invoke(MapMouse(e));
    }

    /// <summary>Feuert das MouseUp‑Event der Abstraktion.</summary>
    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        _mouseUp?.Invoke(MapMouse(e));
    }

    /// <summary>Feuert das MouseWheel‑Event der Abstraktion.</summary>
    protected override void OnMouseWheel(MouseEventArgs e) {
        base.OnMouseWheel(e);
        _mouseWheel?.Invoke(MapMouse(e));
    }

    /// <summary>Konvertiert WinForms MouseEventArgs in das Abstractions‑DTO.</summary>
    private static WindowMouseEvent MapMouse(MouseEventArgs e) {
        var btn = e.Button switch {
            MouseButtons.Left => MouseButton.Left,
            MouseButtons.Right => MouseButton.Right,
            MouseButtons.Middle => MouseButton.Middle,
            _ => MouseButton.None
        };
        return new WindowMouseEvent(e.X, e.Y, btn, e.Delta, e.Clicks);
    }

    /// <summary>Räumt Ressourcen auf und entfernt Abonnenten der Abstractions‑Events.</summary>
    protected override void Dispose(bool disposing) {
        try {
            if (disposing) {
                // Backing‑Events auf null setzen, damit Subscriber nicht länger gehalten werden
                _resized = null;
                _mouseMove = null;
                _mouseDown = null;
                _mouseUp = null;
                _mouseWheel = null;

                _appIcon?.Dispose();
                _appIcon = null;
            }
        }
        finally {
            base.Dispose(disposing);
        }
    }

}
