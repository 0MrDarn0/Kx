// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Abstractions.Events;
using Kx.Abstractions.UI;
using Kx.UI.Elements;
using Kx.UI.VisualTree;
using SkiaSharp;

namespace Kx.UI.Manager;

public class UIElementManager : IUIElementManager {

    private readonly List<IVisual> _elements = [];
    private readonly List<IVisual> _roots = [];

    public IEnumerable<IVisual> Elements => _elements;
    public IEnumerable<IVisual> Roots => _roots;

    public float DpiScale { get; private set; } = 1f;
    public IVisual? FocusedElement { get; private set; }
    public IVisual? ModalElement { get; private set; }

    public void Add(IVisual el) {
        if (el == null)
            return;

        _elements.Add(el);

        // Wenn das Element keinen Parent hat, behandeln wir es als Root
        if (el is not UIElement { Parent: not null } && !_roots.Contains(el))
            _roots.Add(el);

        SortElements();
    }

    public void AddRoot(IVisual root) {
        if (root == null)
            return;
        if (!_elements.Contains(root))
            _elements.Add(root);

        // Root hat explizit keinen Parent
        if (root is UIElement element)
            element.SetParent(null);

        if (!_roots.Contains(root))
            _roots.Add(root);

        SortElements();
    }

    public void Remove(IVisual el) {
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

    public void BringToFront(IVisual el) {
        if (el == null)
            return;
        el.ZIndex = _elements.Count != 0 ? _elements.Max(x => x.ZIndex) + 1 : 0;
        SortElements();
    }

    public void SendToBack(IVisual el) {
        if (el == null)
            return;
        el.ZIndex = _elements.Count != 0 ? _elements.Min(x => x.ZIndex) - 1 : 0;
        SortElements();
    }

    public void SetFocus(IVisual el) {
        if (FocusedElement == el)
            return;

        var previous = FocusedElement;

        if (FocusedElement is Visual oldVisual)
            oldVisual.SetFocused(false);

        previous?.OnFocusLost();

        FocusedElement = el;

        if (el is Visual visual)
            visual.SetFocused(true);

        el.OnFocusGained();
    }

    public void ClearFocus() {
        var previous = FocusedElement;

        if (FocusedElement is Visual oldVisual)
            oldVisual.SetFocused(false);

        previous?.OnFocusLost();

        FocusedElement = null;
    }

    public void ShowModal(IVisual el) {
        ModalElement = el;
        BringToFront(el);
    }

    public void CloseModal() {
        ModalElement = null;
    }

    public void SetDpiScale(float scale) {
        DpiScale = scale;

        foreach (var el in _elements)
            if (el is Visual visual)
                visual.SetDpiScale(scale);
            else
                el.OnDpiChanged(scale);
    }

    public void Render(SKCanvas canvas) {
        // Render in _elements-Reihenfolge (bereits sortiert nach Layer/ZIndex)
        foreach (var el in _elements)
            if (el.Visible)
                el.Draw(canvas);
    }

    private bool HitTest(Point p, Func<IVisual, bool> action) {
        // Iterate roots from topmost to bottommost
        foreach (var root in _roots.OrderByDescending(x => x.Layer).ThenByDescending(x => x.ZIndex)) {
            if (HitTestRecursive(root, p, action))
                return true;
        }
        return false;
    }

    private static bool HitTestRecursive(IVisual el, Point p, Func<IVisual, bool> action) {
        if (!el.Visible)
            return false;

        // Prüfe zuerst Kinder (oberstes Kind zuerst), falls das Element ein Panel/Container ist
        if (el is IVisualContainer container && container.Children.Count != 0) {
            // sortiere Kinder nach Layer/ZIndex absteigend (oberstes zuerst)
            var children = container.Children
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

    public bool KeyDown(KeyCode key) {
        if (FocusedElement != null)
            return FocusedElement.OnKeyDown(key);

        return false;
    }

    public bool KeyUp(KeyCode key) {
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
