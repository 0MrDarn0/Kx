// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.UI.Control;
using SkiaSharp;

namespace KUpdater.UI;

public class ControlManager : IDisposable {
    private readonly List<IControl> _controls = [];
    public void Add(IControl control) => _controls.Add(control);

    public void DisposeAndClearAll() {
        int count = _controls.Count;
        foreach (var control in _controls)
            control.Dispose();
        _controls.Clear();
    }

    public void DisposeAndClear<T>() where T : class, IControl {
        int count = _controls.Count(control => control is T);
        foreach (var control in _controls.OfType<T>())
            control.Dispose();
        _controls.RemoveAll(control => control is T);
    }

    public T? FindById<T>(string id) where T : class, IControl
       => _controls.OfType<T>()
        .FirstOrDefault(control => control.Id == id);

    public void Update<T>(string id, Action<T> callback) where T : class, IControl {
        var control = FindById<T>(id);
        if (control != null)
            callback(control);
    }

    public bool TryUpdate<T>(string id, Action<T> callback) where T : class, IControl {
        var control = FindById<T>(id);
        if (control == null)
            return false;

        callback(control);
        return true;
    }

    public void Draw(SKCanvas canvas) {
        foreach (var control in _controls)
            if (control.Visible)
                control.Draw(canvas);
    }

    public bool MouseMove(Point point) {
        bool needsRedraw = false;
        foreach (var control in _controls.ToList())
            if (control.Visible && control.OnMouseMove(point))
                needsRedraw = true;
        return needsRedraw;
    }

    public bool MouseDown(Point point) {
        bool needsRedraw = false;
        foreach (var control in _controls.ToList())
            if (control.Visible && control.OnMouseDown(point))
                needsRedraw = true;
        return needsRedraw;
    }

    public bool MouseUp(Point point) {
        bool needsRedraw = false;
        foreach (var control in _controls.ToList())
            if (control.Visible && control.OnMouseUp(point))
                needsRedraw = true;
        return needsRedraw;
    }

    public bool MouseWheel(int delta, Point point) {
        bool needsRedraw = false;
        foreach (var control in _controls.ToList())
            if (control.Visible && control.Bounds.Contains(point) && control.OnMouseWheel(delta, point))
                needsRedraw = true;
        return needsRedraw;
    }

    public void Dispose() => DisposeAndClearAll();
}
