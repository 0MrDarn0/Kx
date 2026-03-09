// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Events;
using KUpdater.Abstractions.WindowHost;

namespace KUpdater.Core;

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
        windowHost.Resized += OnResize;
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
}
