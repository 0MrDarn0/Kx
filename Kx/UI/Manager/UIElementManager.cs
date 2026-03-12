// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.UI.Elements;
using SkiaSharp;

namespace Kx.UI.Manager;

public class UIElementManager : IDisposable {

    private readonly List<UIElement> _elements = [];
    private readonly List<UIElement> _roots = [];

    public IEnumerable<UIElement> Elements => _elements;
    public IEnumerable<UIElement> Roots => _roots;

    public float DpiScale { get; private set; } = 1f;
    public UIElement? FocusedElement { get; private set; }
    public UIElement? ModalElement { get; private set; }

    public void Add(UIElement el) {
        if (el == null)
            return;

        _elements.Add(el);

        // Wenn das Element keinen Parent hat, behandeln wir es als Root
        if (el.Parent == null && !_roots.Contains(el))
            _roots.Add(el);

        SortElements();
    }

    public void AddRoot(UIElement root) {
        if (root == null)
            return;
        if (!_elements.Contains(root))
            _elements.Add(root);

        // Root hat explizit keinen Parent
        root.Parent = null;

        if (!_roots.Contains(root))
            _roots.Add(root);

        SortElements();
    }

    public void Remove(UIElement el) {
        if (el == null)
            return;

        _elements.Remove(el);
        _roots.Remove(el);

        el.Dispose();
    }

    private void SortElements() {
        _elements.Sort((a, b) => {
            int layer = a.Layer.CompareTo(b.Layer);
            if (layer != 0)
                return layer;
            return a.ZIndex.CompareTo(b.ZIndex);
        });

        // Roots sollten ebenfalls nach Layer/ZIndex sortiert sein (falls relevant)
        _roots.Sort((a, b) => {
            int layer = a.Layer.CompareTo(b.Layer);
            if (layer != 0)
                return layer;
            return a.ZIndex.CompareTo(b.ZIndex);
        });
    }

    /// <summary>
    /// LayoutAll: arrangiert nur die Root-Elemente.
    /// Der Aufrufer (Renderer) muss das Content-Rect und das Window-Rect übergeben.
    /// - contentRect: Bereich innerhalb des Fensterrahmens (renderer.GetContentRect(...))
    /// - windowRect: gesamtes Fenster (0,0..width,height) für Overlays/Popups
    /// </summary>
    public void LayoutAll(SKRect contentRect, SKRect windowRect) {
        var dpi = DpiScale;

        // Zuerst messen (Bottom-up)
        foreach (var root in _roots)
            root.Measure(dpi);

        // Dann arrangieren (Top-down). Entscheide pro Root, ob ContentArea oder WindowRect verwendet wird.
        foreach (var root in _roots) {
            var target = root.UseContentArea ? contentRect.ToRectangle() : windowRect.ToRectangle();
            root.Arrange(target, dpi);
        }
    }

    public void BringToFront(UIElement el) {
        if (el == null)
            return;
        el.ZIndex = _elements.Count != 0 ? _elements.Max(x => x.ZIndex) + 1 : 0;
        SortElements();
    }

    public void SendToBack(UIElement el) {
        if (el == null)
            return;
        el.ZIndex = _elements.Count != 0 ? _elements.Min(x => x.ZIndex) - 1 : 0;
        SortElements();
    }

    public void SetFocus(UIElement el) {
        if (FocusedElement == el)
            return;

        if (FocusedElement is UIElement old)
            old.OnFocusLost();

        FocusedElement = el;

        if (el is UIElement cb)
            cb.OnFocusGained();
    }

    public void ClearFocus() {
        if (FocusedElement is UIElement old)
            old.OnFocusLost();

        FocusedElement = null;
    }

    public void ShowModal(UIElement el) {
        ModalElement = el;
        BringToFront(el);
    }

    public void CloseModal() {
        ModalElement = null;
    }

    public void SetDpiScale(float scale) {
        DpiScale = scale;

        foreach (var el in _elements)
            el.SetDpiScale(scale);
    }

    public void Render(SKCanvas canvas) {
        // Render in _elements-Reihenfolge (bereits sortiert nach Layer/ZIndex)
        foreach (var el in _elements)
            if (el.Visible)
                el.Draw(canvas);
    }

    private bool HitTest(Point p, Func<UIElement, bool> action) {
        // Iterate roots from topmost to bottommost
        foreach (var root in _roots.OrderByDescending(x => x.Layer).ThenByDescending(x => x.ZIndex)) {
            if (HitTestRecursive(root, p, action))
                return true;
        }
        return false;
    }

    private static bool HitTestRecursive(UIElement el, Point p, Func<UIElement, bool> action) {
        if (!el.Visible)
            return false;

        // Prüfe zuerst Kinder (oberstes Kind zuerst), falls das Element ein Panel/Container ist
        if (el is Elements.Panel.Panel panel && panel.Children.Count != 0) {
            // sortiere Kinder nach Layer/ZIndex absteigend (oberstes zuerst)
            var children = panel.Children
            .OrderByDescending(c => c.Layer)
            .ThenByDescending(c => c.ZIndex);

            foreach (var child in children) {
                if (HitTestRecursive(child, p, action))
                    return true;
            }
        }

        // Wenn kein Kind das Event behandelt hat, prüfe das aktuelle Element selbst
        if (el.Bounds.Contains(p)) {
            try {
                return action(el);
            }
            catch {
                // defensive: Listener darf keine Exceptions durchreichen
                return false;
            }
        }

        return false;
    }


    public bool MouseDown(Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseDown(p);

        return HitTest(p, el => el.OnMouseDown(p));
    }

    public bool MouseUp(Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseUp(p);

        return HitTest(p, el => el.OnMouseUp(p));
    }

    public bool MouseMove(Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseMove(p);

        return HitTest(p, el => el.OnMouseMove(p));
    }

    public bool MouseWheel(int delta, Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseWheel(delta, p);

        return HitTest(p, el => el.OnMouseWheel(delta, p));
    }

    public bool KeyDown(Keys key) {
        if (FocusedElement != null)
            return FocusedElement.OnKeyDown(key);

        return false;
    }

    public bool KeyUp(Keys key) {
        if (FocusedElement != null)
            return FocusedElement.OnKeyUp(key);

        return false;
    }

    public void Dispose() {
        foreach (var el in _elements)
            el.Dispose();

        _elements.Clear();
        _roots.Clear();
    }
}
