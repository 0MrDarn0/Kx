// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.UI.Control;
using SkiaSharp;

namespace KUpdater.UI;

public class ControlManager : IDisposable {
    private readonly List<IControl> _controls = new();

    public float DpiScale { get; private set; } = 1f;

    public IControl? FocusedControl { get; private set; }
    public IControl? ModalControl { get; private set; }

    public void Add(IControl control) {
        _controls.Add(control);
        SortControls();
    }

    public void Remove(IControl control) {
        _controls.Remove(control);
        control.Dispose();
    }

    private void SortControls() {
        _controls.Sort((a, b) => {
            int layer = a.Layer.CompareTo(b.Layer);
            if (layer != 0)
                return layer;
            return a.ZIndex.CompareTo(b.ZIndex);
        });
    }

    public void BringToFront(IControl c) {
        c.ZIndex = _controls.Max(x => x.ZIndex) + 1;
        SortControls();
    }

    public void SendToBack(IControl c) {
        c.ZIndex = _controls.Min(x => x.ZIndex) - 1;
        SortControls();
    }

    public void SetFocus(IControl c) {
        if (FocusedControl == c)
            return;

        if (FocusedControl is ControlBase old)
            old.OnFocusLost();

        FocusedControl = c;

        if (c is ControlBase cb)
            cb.OnFocusGained();
    }

    public void ClearFocus() {
        if (FocusedControl is ControlBase old)
            old.OnFocusLost();

        FocusedControl = null;
    }

    public void ShowModal(IControl c) {
        ModalControl = c;
        BringToFront(c);
    }

    public void CloseModal() {
        ModalControl = null;
    }

    public void SetDpiScale(float scale) {
        DpiScale = scale;

        foreach (var c in _controls)
            if (c is ControlBase cb)
                cb.SetDpiScale(scale);
    }

    public void Render(SKCanvas canvas) {
        foreach (var c in _controls)
            if (c.Visible)
                c.Draw(canvas);
    }

    private bool HitTest(Point p, Func<IControl, bool> action) {
        foreach (var c in _controls.OrderByDescending(x => x.Layer).ThenByDescending(x => x.ZIndex))
            if (c.Visible && c.Bounds.Contains(p) && action(c))
                return true;

        return false;
    }

    public bool MouseDown(Point p) {
        if (ModalControl != null)
            return ModalControl.OnMouseDown(p);

        return HitTest(p, c => c.OnMouseDown(p));
    }

    public bool MouseUp(Point p) {
        if (ModalControl != null)
            return ModalControl.OnMouseUp(p);

        return HitTest(p, c => c.OnMouseUp(p));
    }

    public bool MouseMove(Point p) {
        if (ModalControl != null)
            return ModalControl.OnMouseMove(p);

        return HitTest(p, c => c.OnMouseMove(p));
    }

    public bool MouseWheel(int delta, Point p) {
        if (ModalControl != null)
            return ModalControl.OnMouseWheel(delta, p);

        return HitTest(p, c => c.OnMouseWheel(delta, p));
    }

    public bool KeyDown(Keys key) {
        if (FocusedControl != null)
            return FocusedControl.OnKeyDown(key);

        return false;
    }

    public bool KeyUp(Keys key) {
        if (FocusedControl != null)
            return FocusedControl.OnKeyUp(key);

        return false;
    }

    public void Dispose() {
        foreach (var c in _controls)
            c.Dispose();

        _controls.Clear();
    }
}
