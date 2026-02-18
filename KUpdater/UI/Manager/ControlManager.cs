// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.UI.Control;
using SkiaSharp;

namespace KUpdater.UI;

public class ControlManager : IDisposable {
    private readonly List<IControl> _frameControls = [];
    private readonly List<IControl> _contentControls = [];
    private readonly List<IControl> _overlayControls = [];

    private IEnumerable<IControl> AllControls() {
        foreach (var c in _frameControls)
            yield return c;
        foreach (var c in _contentControls)
            yield return c;
        foreach (var c in _overlayControls)
            yield return c;
    }

    public void Add(IControl control) {
        switch (control.Layer) {
            case ControlLayer.Frame:
            _frameControls.Add(control);
            break;

            case ControlLayer.Content:
            _contentControls.Add(control);
            break;

            case ControlLayer.Overlay:
            _overlayControls.Add(control);
            break;
        }
    }

    private bool HitTest(List<IControl> list, Point p, Func<IControl, bool> action) {
        bool needsRedraw = false;

        foreach (var c in list.ToList())
            if (c.Visible && c.Bounds.Contains(p) && action(c))
                needsRedraw = true;

        return needsRedraw;
    }


    public void DisposeAndClearAll() {
        foreach (var c in AllControls())
            c.Dispose();

        _frameControls.Clear();
        _contentControls.Clear();
        _overlayControls.Clear();
    }

    public void DisposeAndClear<T>() where T : class, IControl {
        foreach (var c in AllControls().OfType<T>())
            c.Dispose();

        _frameControls.RemoveAll(c => c is T);
        _contentControls.RemoveAll(c => c is T);
        _overlayControls.RemoveAll(c => c is T);
    }

    public T? FindById<T>(string id) where T : class, IControl {
        return AllControls().OfType<T>().FirstOrDefault(c => c.Id == id);
    }

    public void Update<T>(string id, Action<T> callback) where T : class, IControl {
        var c = FindById<T>(id);
        if (c != null)
            callback(c);
    }

    public bool TryUpdate<T>(string id, Action<T> callback) where T : class, IControl {
        var c = FindById<T>(id);
        if (c == null)
            return false;

        callback(c);
        return true;
    }

    public void DrawFrameControls(SKCanvas canvas) {
        foreach (var c in _frameControls)
            if (c.Visible)
                c.Draw(canvas);
    }

    public void DrawContentControls(SKCanvas canvas) {
        foreach (var c in _contentControls)
            if (c.Visible)
                c.Draw(canvas);
    }

    public void DrawOverlayControls(SKCanvas canvas) {
        foreach (var c in _overlayControls)
            if (c.Visible)
                c.Draw(canvas);
    }

    public bool MouseDown(Point p) {
        if (HitTest(_overlayControls, p, (c) => c.OnMouseDown(p)))
            return true;
        if (HitTest(_frameControls, p, (c) => c.OnMouseDown(p)))
            return true;
        return HitTest(_contentControls, p, (c) => c.OnMouseDown(p));
    }

    public bool MouseUp(Point p) {
        if (HitTest(_overlayControls, p, (c) => c.OnMouseUp(p)))
            return true;
        if (HitTest(_frameControls, p, (c) => c.OnMouseUp(p)))
            return true;
        return HitTest(_contentControls, p, (c) => c.OnMouseUp(p));
    }

    public bool MouseMove(Point p) {
        if (HitTest(_overlayControls, p, (c) => c.OnMouseMove(p)))
            return true;
        if (HitTest(_frameControls, p, (c) => c.OnMouseMove(p)))
            return true;
        return HitTest(_contentControls, p, (c) => c.OnMouseMove(p));
    }

    public bool MouseWheel(int delta, Point p) {
        if (HitTest(_overlayControls, p, (c) => c.OnMouseWheel(delta, p)))
            return true;
        if (HitTest(_frameControls, p, (c) => c.OnMouseWheel(delta, p)))
            return true;
        return HitTest(_contentControls, p, (c) => c.OnMouseWheel(delta, p));
    }


    public void Dispose() => DisposeAndClearAll();
}
