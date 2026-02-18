// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.UI.Interface;
using SkiaSharp;

namespace KUpdater.UI.Control;

public abstract class ControlBase : IControl, IDisposable {
    public string Id { get; }
    protected readonly WindowContext _ctx;
    protected readonly IUiThreadInvoker _ui;
    protected readonly Func<Rectangle> _boundsFunc;
    public ControlLayer Layer { get; set; } = ControlLayer.Content;

    protected bool _initializing = true;
    private bool _disposed;

    public Rectangle Bounds => _boundsFunc();

    protected readonly Property<bool> _visible;
    public bool Visible {
        get => _visible.Value;
        set => _visible.Value = value;
    }

    protected ControlBase(WindowContext ctx, string id, Func<Rectangle> boundsFunc, bool visible = true) {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _ui = _ctx.UiThread;
        Id = id ?? throw new ArgumentNullException(nameof(id));
        _boundsFunc = boundsFunc ?? throw new ArgumentNullException(nameof(boundsFunc));
        _visible = new Property<bool>(_ui, visible, () => { if (!_initializing) _ctx.Renderer.RequestRender(); });
        _initializing = false;
    }

    // Zeichnen: abgeleitete Klassen implementieren
    public abstract void Draw(SKCanvas canvas);

    // Mausereignisse: Standardimplementierung false
    public virtual bool OnMouseMove(Point p) => false;
    public virtual bool OnMouseDown(Point p) => false;
    public virtual bool OnMouseUp(Point p) => false;
    public virtual bool OnMouseWheel(int delta, Point p) => false;

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;
        if (disposing) {
            // abgeleitete Klassen können hier managed Ressourcen freigeben
        }
        _disposed = true;
    }

    // Hilfsmethode für abgeleitete Klassen um Render anzufordern
    //protected void Invalidate() => _ctx.Renderer.RequestRender();
    protected void Invalidate() { if (!_initializing) _ctx.Renderer.RequestRender(); }
}
