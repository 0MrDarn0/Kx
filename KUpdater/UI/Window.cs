// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Interop;

namespace KUpdater.UI;

public abstract class Window : Form {
    protected bool _isDragging = false;
    protected Point _dragStart;

    protected bool _isResizing = false;
    protected Point _resizeStartCursor;
    protected Size _resizeStartSize;
    protected readonly int _resizeHitSize = 40;

    protected virtual int MinClientWidth => 450;
    protected virtual int MinClientHeight => 300;

    protected Window() {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
    }

    protected abstract void RequestRender();
    protected virtual bool OnChildMouseMove(MouseEventArgs e) => false;
    protected virtual bool OnChildMouseDown(MouseEventArgs e) => false;
    protected virtual bool OnChildMouseUp(MouseEventArgs e) => false;
    protected virtual bool OnChildMouseWheel(MouseEventArgs e) => false;

    protected override CreateParams CreateParams {
        get {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
            return cp;
        }
    }

    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        RequestRender();
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        if (_isResizing) {
            Point delta = new(
               Cursor.Position.X - _resizeStartCursor.X,
               Cursor.Position.Y - _resizeStartCursor.Y);

            Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;

            int maxWidth = workArea.Width;
            int maxHeight = workArea.Height;

            int newWidth = _resizeStartSize.Width + delta.X;
            int newHeight = _resizeStartSize.Height + delta.Y;

            newWidth = Math.Max(MinClientWidth, Math.Min(newWidth, maxWidth));
            newHeight = Math.Max(MinClientHeight, Math.Min(newHeight, maxHeight));

            this.Size = new Size(newWidth, newHeight);
            return;
        }

        if (_isDragging) {
            Point newLocation = new(this.Left + e.X - _dragStart.X, this.Top + e.Y - _dragStart.Y);
            this.Location = newLocation;
            return;
        }

        this.Cursor = new Rectangle(
            this.Width - _resizeHitSize,
            this.Height - _resizeHitSize,
            _resizeHitSize,
            _resizeHitSize
        ).Contains(e.Location) ? Cursors.SizeNWSE : Cursors.Default;


        if (OnChildMouseMove(e))
            RequestRender();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        if (e.Button != MouseButtons.Left)
            return;

        if (OnChildMouseDown(e)) {
            RequestRender();
            return;
        }

        Rectangle resizeRect = new(this.Width - _resizeHitSize, this.Height - _resizeHitSize, _resizeHitSize, _resizeHitSize);
        if (resizeRect.Contains(e.Location)) {
            _isResizing = true;
            _resizeStartCursor = Cursor.Position;
            _resizeStartSize = this.Size;
            return;
        }

        _isDragging = true;
        _dragStart = e.Location;
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        _isDragging = false;
        _isResizing = false;

        if (OnChildMouseUp(e))
            RequestRender();
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
        if (OnChildMouseWheel(e))
            RequestRender();
    }
}
