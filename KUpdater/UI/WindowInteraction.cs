// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.UI.Interface;

namespace KUpdater.UI;

public class WindowInteraction {
    private readonly IWindowBackend _backend;
    private readonly WindowContext _ctx;

    private bool _isDragging;
    private Point _dragStart;

    private bool _isResizing;
    private Point _resizeStartCursor;
    private Size _resizeStartSize;
    private readonly int _resizeHitSize = 40;

    private readonly int _minClientWidth;
    private readonly int _minClientHeight;

    public WindowInteraction(IWindowBackend backend, WindowContext ctx,
        int minClientWidth = 450, int minClientHeight = 300) {
        _backend = backend;
        _ctx = ctx;
        _minClientWidth = minClientWidth;
        _minClientHeight = minClientHeight;

        backend.BackendMouseMove += OnMouseMove;
        backend.BackendMouseDown += OnMouseDown;
        backend.BackendMouseUp += OnMouseUp;
        backend.BackendMouseWheel += OnMouseWheel;
        backend.BackendResized += OnResize;
    }

    private void OnResize(int w, int h) {
        _ctx.Renderer.RequestRender();
    }

    private void OnMouseMove(MouseEventArgs e) {
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

            _backend.SetSize(newWidth, newHeight);
            return;
        }

        if (_isDragging) {
            Point newLocation = new(
                _backend.Left + e.X - _dragStart.X,
                _backend.Top + e.Y - _dragStart.Y);
            _backend.SetPosition(newLocation.X, newLocation.Y);
            return;
        }

        var resizeRect = new Rectangle(
            _backend.Width - _resizeHitSize,
            _backend.Height - _resizeHitSize,
            _resizeHitSize,
            _resizeHitSize);

        _backend.Cursor = resizeRect.Contains(e.Location)
            ? Cursors.SizeNWSE
            : Cursors.Default;

        if (_ctx.Controls.MouseMove(e.Location))
            _ctx.Renderer.RequestRender();
    }

    private void OnMouseDown(MouseEventArgs e) {
        if (e.Button != MouseButtons.Left)
            return;

        if (_ctx.Controls.MouseDown(e.Location)) {
            _ctx.Renderer.RequestRender();
            return;
        }

        Rectangle resizeRect = new(
            _backend.Width - _resizeHitSize,
            _backend.Height - _resizeHitSize,
            _resizeHitSize,
            _resizeHitSize);

        if (resizeRect.Contains(e.Location)) {
            _isResizing = true;
            _resizeStartCursor = Cursor.Position;
            _resizeStartSize = new Size(_backend.Width, _backend.Height);
            return;
        }

        _isDragging = true;
        _dragStart = e.Location;
    }

    private void OnMouseUp(MouseEventArgs e) {
        _isDragging = false;
        _isResizing = false;

        if (_ctx.Controls.MouseUp(e.Location))
            _ctx.Renderer.RequestRender();
    }

    private void OnMouseWheel(MouseEventArgs e) {
        if (_ctx.Controls.MouseWheel(e.Delta, e.Location))
            _ctx.Renderer.RequestRender();
    }
}
