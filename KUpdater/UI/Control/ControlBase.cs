// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Backend.BackendAbstractions;
using KUpdater.Core;
using KUpdater.UI.Binding;
using SkiaSharp;

namespace KUpdater.UI.Control;

public abstract class ControlBase : IControl, IDisposable {
    public string Id { get; }
    protected readonly WindowContext _ctx;
    protected readonly IUiThreadInvoker _ui;

    public ControlLayer Layer { get; set; } = ControlLayer.Content;

    public int ZIndex { get; set; } = 0;

    public virtual bool CanFocus => false;
    public bool IsFocused { get; internal set; }

    public float DpiScale { get; private set; } = 1f;

    public virtual void OnDpiChanged(float scale) { }
    public virtual void OnFocusGained() { }
    public virtual void OnFocusLost() { }

    public virtual bool OnKeyDown(Keys key) => false;
    public virtual bool OnKeyUp(Keys key) => false;

    protected readonly Func<Rectangle> _boundsFunc;
    public Rectangle Bounds => _boundsFunc();

    protected readonly Property<bool> _visible;
    public bool Visible {
        get => _visible.Value;
        set => _visible.Value = value;
    }

    protected bool _initializing = true;
    private bool _disposed;

    protected ControlBase(WindowContext ctx, string id, Func<Rectangle> boundsFunc, bool visible = true) {
        _ctx = ctx;
        _ui = ctx.UiThread;
        Id = id;
        _boundsFunc = boundsFunc;

        _visible = new Property<bool>(_ui, visible, () => Invalidate());
        _initializing = false;
    }

    public virtual void Draw(SKCanvas canvas) { }

    public virtual bool OnMouseMove(Point p) => false;
    public virtual bool OnMouseDown(Point p) => false;
    public virtual bool OnMouseUp(Point p) => false;
    public virtual bool OnMouseWheel(int delta, Point p) => false;

    protected void Invalidate() {
        if (!_initializing)
            _ctx.Renderer.RequestRender();
    }

    internal void SetDpiScale(float scale) {
        DpiScale = scale;
        OnDpiChanged(scale);
        Invalidate();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        _disposed = true;
    }
}
