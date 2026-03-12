// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.Abstractions.Events;
using Kx.Abstractions.WindowHost;
using Kx.Core.Interop;
using Kx.Core.Localization;
using Kx.Utility;

namespace Kx.WindowHost.WinForms;

public class WinFormsWindowHost : Form, IWindowHost {
    IntPtr IWindowSurface.Handle => Handle;
    bool IWindowSurface.IsDisposed => IsDisposed;
    bool IWindowSurface.IsHandleCreated => IsHandleCreated;
    int IWindowSurface.Left => Left;
    int IWindowSurface.Top => Top;
    int IWindowSurface.Width => Width;
    int IWindowSurface.Height => Height;
    int IWindowSurface.DeviceDpi => DeviceDpi;
    bool IUiDispatcher.InvokeRequired => base.InvokeRequired;
    void IUiDispatcher.BeginInvoke(Delegate d) => base.BeginInvoke(d);
    void IUiDispatcher.Invoke(Action action) => base.Invoke(action);

    private Icon? _appIcon;
    private bool _iconMissingNotified;

    private Keys _lastKeyDown = Keys.None;
    private DateTime _lastKeyDownTime = DateTime.MinValue;
    private WindowState _lastState = Abstractions.Events.WindowState.Restored;

    private event Action<ResizeEvent>? _resized;
    private event Action<MouseEvent>? _mouseMove;
    private event Action<MouseEvent>? _mouseDown;
    private event Action<MouseEvent>? _mouseUp;
    private event Action<MouseEvent>? _mouseWheel;
    private event Action<ShownEvent>? _shown;
    private event Action<ClosedEvent>? _closed;
    private event Action<KeyEvent>? _keyDown;
    private event Action<KeyEvent>? _keyUp;
    private event Action<TextInputEvent>? _textInput;
    private event Action<StateEvent>? _stateChanged;
    private event Action<FocusEvent>? _focusChanged;


    event Action<ResizeEvent>? IWindowHost.Resized { add => _resized += value; remove => _resized -= value; }
    event Action<MouseEvent>? IWindowHost.MouseMove { add => _mouseMove += value; remove => _mouseMove -= value; }
    event Action<MouseEvent>? IWindowHost.MouseDown { add => _mouseDown += value; remove => _mouseDown -= value; }
    event Action<MouseEvent>? IWindowHost.MouseUp { add => _mouseUp += value; remove => _mouseUp -= value; }
    event Action<MouseEvent>? IWindowHost.MouseWheel { add => _mouseWheel += value; remove => _mouseWheel -= value; }
    event Action<ShownEvent>? IWindowHost.Shown { add => _shown += value; remove => _shown -= value; }
    event Action<ClosedEvent>? IWindowHost.Closed { add => _closed += value; remove => _closed -= value; }
    event Action<KeyEvent>? IWindowHost.KeyDown { add => _keyDown += value; remove => _keyDown -= value; }
    event Action<KeyEvent>? IWindowHost.KeyUp { add => _keyUp += value; remove => _keyUp -= value; }
    event Action<TextInputEvent>? IWindowHost.TextInput { add => _textInput += value; remove => _textInput -= value; }
    event Action<StateEvent>? IWindowHost.StateChanged { add => _stateChanged += value; remove => _stateChanged -= value; }
    event Action<FocusEvent>? IWindowHost.FocusChanged { add => _focusChanged += value; remove => _focusChanged -= value; }

    public void SetSize(int width, int height) => Size = new Size(width, height);
    public void SetPosition(int x, int y) => Location = new Point(x, y);
    public void ShowWindow() => Show();
    public void CloseWindow() => Close();

    object? IWindowHost.Cursor {
        get => base.Cursor;
        set => base.Cursor = value as Cursor;
    }

    public WinFormsWindowHost() {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        Width = 950;
        Height = 600;
    }

    protected override CreateParams CreateParams {
        get {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);

        try {
            var path = Paths.GetResource("Default\\app.ico");
            if (File.Exists(path)) {
                _appIcon = new Icon(path);
                Icon = _appIcon;

                NativeMethods.SendMessage(Handle, NativeMethods.WM_SETICON, new IntPtr(NativeMethods.ICON_BIG), _appIcon.Handle);
                NativeMethods.SendMessage(Handle, NativeMethods.WM_SETICON, new IntPtr(NativeMethods.ICON_SMALL), _appIcon.Handle);
                NativeMethods.SetClassLongPtr(Handle, NativeMethods.GCL_HICON, _appIcon.Handle);
                NativeMethods.SetClassLongPtr(Handle, NativeMethods.GCL_HICONSM, _appIcon.Handle);
            }
            else if (!_iconMissingNotified) {
                _iconMissingNotified = true;
                MessageBox.Show(
                    this,
                    LanguageService.Translate("dialog.app_icon_missing.message", path),
                    LanguageService.Translate("dialog.app_icon_missing.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"Fehler beim Laden des App-Icons: {ex}");
        }
    }

    protected override void OnResize(EventArgs e) {
        base.OnResize(e);

        _resized?.Invoke(new ResizeEvent(Width, Height));

        var newState = WindowState switch {
            FormWindowState.Minimized => Abstractions.Events.WindowState.Minimized,
            FormWindowState.Maximized => Abstractions.Events.WindowState.Maximized,
            _ => Abstractions.Events.WindowState.Restored
        };

        if (newState != _lastState) {
            _lastState = newState;
            _stateChanged?.Invoke(new StateEvent(newState));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        _mouseMove?.Invoke(MapMouse(e));
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        _mouseDown?.Invoke(MapMouse(e));
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        _mouseUp?.Invoke(MapMouse(e));
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
        base.OnMouseWheel(e);
        _mouseWheel?.Invoke(MapMouse(e));
    }

    protected override void OnShown(EventArgs e) {
        base.OnShown(e);
        _shown?.Invoke(new ShownEvent());
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        base.OnFormClosed(e);
        _closed?.Invoke(new ClosedEvent(e.CloseReason == CloseReason.UserClosing));
    }

    protected override void OnActivated(EventArgs e) {
        base.OnActivated(e);
        _focusChanged?.Invoke(new FocusEvent(FocusState.Focused));
    }

    protected override void OnDeactivate(EventArgs e) {
        base.OnDeactivate(e);
        _focusChanged?.Invoke(new FocusEvent(FocusState.LostFocus));
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);

        bool isRepeat = false;

        if (e.KeyCode == _lastKeyDown) {
            var delta = DateTime.Now - _lastKeyDownTime;
            if (delta.TotalMilliseconds < 100)
                isRepeat = true;
        }

        _lastKeyDown = e.KeyCode;
        _lastKeyDownTime = DateTime.Now;

        _keyDown?.Invoke(new KeyEvent(MapKey(e.KeyCode), isRepeat));
    }

    protected override void OnKeyUp(KeyEventArgs e) {
        base.OnKeyUp(e);

        _keyUp?.Invoke(new KeyEvent(MapKey(e.KeyCode), false));

        if (e.KeyCode == _lastKeyDown)
            _lastKeyDown = Keys.None;
    }

    protected override void OnKeyPress(KeyPressEventArgs e) {
        base.OnKeyPress(e);
        _textInput?.Invoke(new TextInputEvent(e.KeyChar));
    }

    private static MouseEvent MapMouse(MouseEventArgs e) {
        var btn = e.Button switch {
            MouseButtons.Left => MouseButton.Left,
            MouseButtons.Right => MouseButton.Right,
            MouseButtons.Middle => MouseButton.Middle,
            _ => MouseButton.None
        };

        return new MouseEvent(e.X, e.Y, btn, e.Delta, e.Clicks);
    }

    private static KeyCode MapKey(Keys key) {
        return key switch {
            Keys.A => KeyCode.A,
            Keys.B => KeyCode.B,
            Keys.C => KeyCode.C,
            Keys.D => KeyCode.D,
            Keys.E => KeyCode.E,
            Keys.F => KeyCode.F,
            Keys.G => KeyCode.G,
            Keys.H => KeyCode.H,
            Keys.I => KeyCode.I,
            Keys.J => KeyCode.J,
            Keys.K => KeyCode.K,
            Keys.L => KeyCode.L,
            Keys.M => KeyCode.M,
            Keys.N => KeyCode.N,
            Keys.O => KeyCode.O,
            Keys.P => KeyCode.P,
            Keys.Q => KeyCode.Q,
            Keys.R => KeyCode.R,
            Keys.S => KeyCode.S,
            Keys.T => KeyCode.T,
            Keys.U => KeyCode.U,
            Keys.V => KeyCode.V,
            Keys.W => KeyCode.W,
            Keys.X => KeyCode.X,
            Keys.Y => KeyCode.Y,
            Keys.Z => KeyCode.Z,

            Keys.D0 => KeyCode.D0,
            Keys.D1 => KeyCode.D1,
            Keys.D2 => KeyCode.D2,
            Keys.D3 => KeyCode.D3,
            Keys.D4 => KeyCode.D4,
            Keys.D5 => KeyCode.D5,
            Keys.D6 => KeyCode.D6,
            Keys.D7 => KeyCode.D7,
            Keys.D8 => KeyCode.D8,
            Keys.D9 => KeyCode.D9,

            Keys.Enter => KeyCode.Enter,
            Keys.Escape => KeyCode.Escape,
            Keys.Space => KeyCode.Space,
            Keys.Back => KeyCode.Backspace,
            Keys.Tab => KeyCode.Tab,
            Keys.ShiftKey => KeyCode.Shift,
            Keys.ControlKey => KeyCode.Control,
            Keys.Menu => KeyCode.Alt,
            Keys.Left => KeyCode.Left,
            Keys.Right => KeyCode.Right,
            Keys.Up => KeyCode.Up,
            Keys.Down => KeyCode.Down,

            _ => KeyCode.None
        };
    }

    protected override void Dispose(bool disposing) {
        try {
            if (disposing) {
                _resized = null;
                _mouseMove = null;
                _mouseDown = null;
                _mouseUp = null;
                _mouseWheel = null;
                _shown = null;
                _closed = null;
                _keyDown = null;
                _keyUp = null;
                _textInput = null;

                _appIcon?.Dispose();
                _appIcon = null;
            }
        }
        finally {
            base.Dispose(disposing);
        }
    }
}
