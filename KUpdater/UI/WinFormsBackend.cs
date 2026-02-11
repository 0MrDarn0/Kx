// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.ComponentModel;
using KUpdater.Interop;
using KUpdater.UI.Interface;
using KUpdater.Utility;
namespace KUpdater.UI;

public class WinFormsBackend : Form, IWindowBackend {
    // IRenderTarget
    IntPtr IRenderTarget.Handle => Handle;
    bool IRenderTarget.IsDisposed => IsDisposed;
    bool IRenderTarget.IsHandleCreated => IsHandleCreated;
    int IRenderTarget.Left => Left;
    int IRenderTarget.Top => Top;
    int IRenderTarget.Width => Width;
    int IRenderTarget.Height => Height;
    int IRenderTarget.DeviceDpi => DeviceDpi;

    // IUiThreadInvoker
    bool IUiThreadInvoker.InvokeRequired => base.InvokeRequired;
    void IUiThreadInvoker.BeginInvoke(Delegate d) => base.BeginInvoke(d);

    // IWindowBackend
    public event Action<int, int>? BackendResized;
    public event Action<MouseEventArgs>? BackendMouseMove;
    public event Action<MouseEventArgs>? BackendMouseDown;
    public event Action<MouseEventArgs>? BackendMouseUp;
    public event Action<MouseEventArgs>? BackendMouseWheel;

    public void SetSize(int width, int height) => Size = new Size(width, height);
    public void SetPosition(int x, int y) => Location = new Point(x, y);

    public override Cursor? Cursor {
        get => base.Cursor;
        set => base.Cursor = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public IHotkeyMessageSink? HotkeySink { get; set; }


    public WinFormsBackend() {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        Width = 900;
        Height = 600;
    }
    protected override CreateParams CreateParams {
        get {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
            return cp;
        }
    }

    protected override void WndProc(ref Message m) {
        if (HotkeySink?.ProcessWndProc(ref m) == true)
            return;

        base.WndProc(ref m);
    }

    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        BackendResized?.Invoke(Width, Height);
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        BackendMouseMove?.Invoke(e);
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        BackendMouseDown?.Invoke(e);
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        BackendMouseUp?.Invoke(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
        base.OnMouseWheel(e);
        BackendMouseWheel?.Invoke(e);
    }
}
