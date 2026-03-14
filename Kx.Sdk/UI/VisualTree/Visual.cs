// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Sdk.Events;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Binding;

using SkiaSharp;

namespace Kx.Sdk.UI.VisualTree;

public abstract class Visual : IVisual, IDisposable {
    public string Id { get; }
    public virtual Rectangle Bounds => Rectangle.Empty;
    public virtual bool UseContentArea { get; set; } = true;

    private readonly Property<bool> _visible;
    public bool Visible {
        get => _visible.Value;
        set => _visible.Value = value;
    }

    private readonly Property<int> _zIndex;
    public int ZIndex {
        get => _zIndex.Value;
        set => _zIndex.Value = value;
    }

    private readonly Property<VisualLayer> _layer;
    private readonly IVisualContext _ctx;
    private readonly List<IDisposable> _trackedDisposables = [];

    public VisualLayer Layer {
        get => _layer.Value;
        set => _layer.Value = value;
    }

    public IVisualContext Context => _ctx;

    public virtual bool CanFocus => false;
    public bool IsFocused { get; private set; }
    public float DpiScale { get; private set; } = 1f;
    public virtual void OnDpiChanged(float scale) { }
    public virtual void OnFocusGained() { }
    public virtual void OnFocusLost() { }
    public virtual bool OnKeyDown(KeyCode key) => false;
    public virtual bool OnKeyUp(KeyCode key) => false;

    protected bool _initializing = true;
    private bool _disposed;

    protected Visual(IVisualContext ctx, string id, bool visible = true) {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _ctx = ctx;
        DpiScale = _ctx.DpiScale;
        Id = id;
        _visible = new Property<bool>(ctx.UiThread, visible, Invalidate);
        _zIndex = new Property<int>(ctx.UiThread, 0, Invalidate);
        _layer = new Property<VisualLayer>(ctx.UiThread, VisualLayer.Content, Invalidate);
        _initializing = false;
    }

    public virtual void Measure(float dpi) { }
    public virtual void Arrange(Rectangle rect, float dpi) { }
    public virtual void Draw(SKCanvas canvas) { }
    public virtual bool OnMouseMove(Point p) => false;
    public virtual bool OnMouseDown(Point p) => false;
    public virtual bool OnMouseUp(Point p) => false;
    public virtual bool OnMouseWheel(int delta, Point p) => false;

    protected void Invalidate() {
        if (!_initializing)
            _ctx.RequestRender();
    }

    public void SetDpiScale(float scale) {
        DpiScale = scale;
        OnDpiChanged(scale);
        Invalidate();
    }

    public void SetFocused(bool isFocused) {
        IsFocused = isFocused;
    }

    /// <summary>
    /// Tracks a disposable resource for the lifetime of this visual.
    /// </summary>
    public void TrackDisposable(IDisposable disposable) {
        ArgumentNullException.ThrowIfNull(disposable);

        if (_disposed) {
            disposable.Dispose();
            return;
        }

        _trackedDisposables.Add(disposable);
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            foreach (var disposable in _trackedDisposables)
                disposable.Dispose();

            _trackedDisposables.Clear();
        }

        _disposed = true;
    }
}
