// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Sdk.UI.Layout;
using Kx.Sdk.WindowHost;

namespace Kx.App;

public class WindowInteraction {
    private readonly IWindowHost _windowHost;
    private readonly WindowContext _ctx;

    private bool _isDragging;
    private Point _dragStart;

    private bool _isResizing;
    private Point _resizeStartCursor;
    private Size _resizeStartSize;
    private readonly int _resizeHitSize = 40;
    private bool _allowResizing = true;

    private readonly int _minClientWidth;
    private readonly int _minClientHeight;
    private bool _isShiftDown;
    private bool _isControlDown;

    public WindowInteraction(IWindowHost windowHost, WindowContext ctx, bool allowResizing = true, int minClientWidth = 450, int minClientHeight = 300) {
        _windowHost = windowHost;
        _ctx = ctx;
        _minClientWidth = minClientWidth;
        _minClientHeight = minClientHeight;
        _allowResizing = allowResizing;

        windowHost.MouseMove += OnMouseMove;
        windowHost.MouseDown += OnMouseDown;
        windowHost.MouseUp += OnMouseUp;
        windowHost.MouseWheel += OnMouseWheel;
        windowHost.KeyDown += OnKeyDown;
        windowHost.KeyUp += OnKeyUp;
        windowHost.Resized += OnResize;
        windowHost.TextInput += OnTextInput;
    }

    private void OnResize(ResizeEvent e) {
        _ctx.Renderer.RequestRender();
    }

    private void OnMouseMove(MouseEvent e) {
        var location = new Point(e.X, e.Y);

        if (_isResizing) {
            Point delta = new(
                Cursor.Position.X - _resizeStartCursor.X,
                Cursor.Position.Y - _resizeStartCursor.Y);

            Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;

            int maxWidth = workArea.Width;
            int maxHeight = workArea.Height;

            int newWidth = _resizeStartSize.Width + delta.X;
            int newHeight = _resizeStartSize.Height + delta.Y;

            newWidth = Math.Max(_minClientWidth, Math.Min(newWidth, maxWidth));
            newHeight = Math.Max(_minClientHeight, Math.Min(newHeight, maxHeight));

            _windowHost.SetSize(newWidth, newHeight);
            return;
        }

        if (_isDragging) {
            Point newLocation = new(
                _windowHost.Left + e.X - _dragStart.X,
                _windowHost.Top + e.Y - _dragStart.Y);
            _windowHost.SetPosition(newLocation.X, newLocation.Y);
            return;
        }

        if (_allowResizing) {
            var resizeRect = new Rectangle(
                _windowHost.Width - _resizeHitSize,
                _windowHost.Height - _resizeHitSize,
                _resizeHitSize,
                _resizeHitSize);

            _windowHost.Cursor = resizeRect.Contains(location)
                ? Cursors.SizeNWSE
                : Cursors.Default;
        }

        if (_ctx.UIElementManager.MouseMove(location))
            _ctx.Renderer.RequestRender();
    }

    private void OnMouseDown(MouseEvent e) {
        if (e.Button != MouseButton.Left)
            return;

        var location = new Point(e.X, e.Y);

        if (_ctx.Frame?.UsesDefaultFrame == true) {
            var closeRect = _ctx.Frame.GetCloseButtonRect(new Size(_windowHost.Width, _windowHost.Height));
            if (closeRect.Contains(location.X, location.Y)) {
                _windowHost.CloseWindow();
                return;
            }
        }

        if (_ctx.UIElementManager.MouseDown(location)) {
            _ctx.Renderer.RequestRender();
            return;
        }

        Rectangle resizeRect = new(
            _windowHost.Width - _resizeHitSize,
            _windowHost.Height - _resizeHitSize,
            _resizeHitSize,
            _resizeHitSize);

        if (_allowResizing && resizeRect.Contains(location)) {
            _isResizing = true;
            _resizeStartCursor = Cursor.Position;
            _resizeStartSize = new Size(_windowHost.Width, _windowHost.Height);
            return;
        }

        if (_ctx.Frame?.UsesDefaultFrame == true) {
            var titleBarRect = _ctx.Frame.GetTitleBarRect(new Size(_windowHost.Width, _windowHost.Height));
            if (!titleBarRect.Contains(location.X, location.Y))
                return;
        }

        _isDragging = true;
        _dragStart = location;
    }

    private void OnMouseUp(MouseEvent e) {
        _isDragging = false;
        _isResizing = false;

        var location = new Point(e.X, e.Y);
        if (_ctx.UIElementManager.MouseUp(location))
            _ctx.Renderer.RequestRender();
    }

    private void OnMouseWheel(MouseEvent e) {
        var location = new Point(e.X, e.Y);
        if (_ctx.UIElementManager.MouseWheel(e.Delta, location))
            _ctx.Renderer.RequestRender();
    }

    private void OnKeyDown(KeyEvent e) {
        UpdateModifierState(e.Key, isDown: true);

        if (!e.IsRepeat && TryHandleDebugOverlayHotkey(e.Key)) {
            _ctx.Renderer.RequestRender();
            return;
        }

        if (_ctx.UIElementManager.KeyDown(e.Key))
            _ctx.Renderer.RequestRender();

        if (_isControlDown && !e.IsRepeat) {
            switch (e.Key) {
                case KeyCode.C:
                    if (_ctx.UIElementManager.Copy())
                        _ctx.Renderer.RequestRender();
                    break;
                case KeyCode.V:
                    if (_ctx.UIElementManager.PasteFromClipboard())
                        _ctx.Renderer.RequestRender();
                    break;
                case KeyCode.X:
                    if (_ctx.UIElementManager.Cut())
                        _ctx.Renderer.RequestRender();
                    break;
                case KeyCode.A:
                    if (_ctx.UIElementManager.SelectAll())
                        _ctx.Renderer.RequestRender();
                    break;
                case KeyCode.Z:
                    if (_ctx.UIElementManager.Undo())
                        _ctx.Renderer.RequestRender();
                    break;
                case KeyCode.Y:
                    if (_ctx.UIElementManager.Redo())
                        _ctx.Renderer.RequestRender();
                    break;
                case KeyCode.Backspace:
                    if ((_isControlDown && _ctx.UIElementManager.DeleteWordLeft()))
                        _ctx.Renderer.RequestRender();
                    break;
                case KeyCode.Delete:
                    if ((_isControlDown && _ctx.UIElementManager.DeleteWordRight()))
                        _ctx.Renderer.RequestRender();
                    break;
            }
        }
    }

    private void OnKeyUp(KeyEvent e) {
        UpdateModifierState(e.Key, isDown: false);

        if (_ctx.UIElementManager.KeyUp(e.Key))
            _ctx.Renderer.RequestRender();
    }

    private void OnTextInput(TextInputEvent e) {
        if (char.IsControl(e.Character))
            return;

        if (_ctx.UIElementManager.TextInput(e.Character.ToString()))
            _ctx.Renderer.RequestRender();
    }

    private bool TryHandleDebugOverlayHotkey(KeyCode key) {
        if (!_isControlDown || !_isShiftDown)
            return false;

        switch (key) {
            case KeyCode.D:
                DebugOverlay.CyclePreset();
                return true;

            case KeyCode.D0:
                DebugOverlay.ApplyPreset(DebugOverlay.OverlayPreset.Off);
                return true;

            case KeyCode.D1:
                DebugOverlay.ApplyPreset(DebugOverlay.OverlayPreset.Layout);
                return true;

            case KeyCode.D2:
                DebugOverlay.ApplyPreset(DebugOverlay.OverlayPreset.Hierarchy);
                return true;

            case KeyCode.D3:
                DebugOverlay.ApplyPreset(DebugOverlay.OverlayPreset.Inspect);
                return true;

            default:
                return false;
        }
    }

    private void UpdateModifierState(KeyCode key, bool isDown) {
        switch (key) {
            case KeyCode.Control:
                _isControlDown = isDown;
                break;

            case KeyCode.Shift:
                _isShiftDown = isDown;
                break;
        }
    }
}
