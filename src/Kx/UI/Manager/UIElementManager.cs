// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Events;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.VisualTree;

using SkiaSharp;

namespace Kx.UI.Manager;

public class UIElementManager : IUIElementManager {

    private readonly List<IVisual> _elements = [];
    private readonly List<IVisual> _roots = [];
    private IVisual? _hoveredElement;
    private IVisual? _capturedElement;

    public IEnumerable<IVisual> Elements => _elements;
    public IEnumerable<IVisual> Roots => _roots;
    public IVisual? HoveredElement => _hoveredElement;
    public IVisual? FocusedElement { get; private set; }
    public IVisual? ModalElement { get; private set; }
    public float DpiScale { get; private set; } = 1f;


    public void Add(IVisual el) {
        if (el == null)
            return;

        _elements.Add(el);

        if (el is not UIElement { Parent: not null } && !_roots.Contains(el))
            _roots.Add(el);

        SortElements();
    }

    public bool TryGet(string id, out IVisual? visual) {
        if (string.IsNullOrWhiteSpace(id)) {
            visual = null;
            return false;
        }

        foreach (var root in _roots) {
            if (TryGetRecursive(root, id, out visual))
                return true;
        }

        visual = null;
        return false;
    }

    public void AddRoot(IVisual root) {
        if (root == null)
            return;
        if (!_elements.Contains(root))
            _elements.Add(root);

        if (root is UIElement element)
            element.SetParent(null);

        if (!_roots.Contains(root))
            _roots.Add(root);

        SortElements();
    }

    public void Remove(IVisual el) {
        if (el == null)
            return;

        ClearTrackedElement(ref _hoveredElement, el);
        ClearTrackedElement(ref _capturedElement, el);

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

        foreach (var root in _roots)
            root.Measure(dpi);

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
        foreach (var el in _elements)
            if (el.Visible)
                el.Draw(canvas);
    }

    private IVisual? HitTest(Point p) {
        foreach (var root in EnumerateHitTestRoots()) {
            var hit = HitTestRecursive(root, p);
            if (hit is not null)
                return hit;
        }

        return null;
    }

    private static bool TryGetRecursive(IVisual visual, string id, out IVisual? match) {
        if (string.Equals(visual.Id, id, StringComparison.Ordinal)) {
            match = visual;
            return true;
        }

        if (visual is IVisualContainer container) {
            foreach (var child in container.Children) {
                if (TryGetRecursive(child, id, out match))
                    return true;
            }
        }

        match = null;
        return false;
    }

    private static IVisual? HitTestRecursive(IVisual el, Point p) {
        if (!el.Visible)
            return null;

        if (el is IVisualContainer container && container.Children.Count != 0) {
            var children = container.Children
                .OrderByDescending(c => c.Layer)
                .ThenByDescending(c => c.ZIndex);

            foreach (var child in children) {
                var hit = HitTestRecursive(child, p);
                if (hit is not null)
                    return hit;
            }
        }

        return el.Bounds.Contains(p)
            ? el
            : null;
    }

    private bool TryDispatchHit(Point p, Func<IVisual, bool> action, out IVisual? handledElement) {
        foreach (var root in EnumerateHitTestRoots()) {
            if (TryDispatchHitRecursive(root, p, action, out handledElement))
                return true;
        }

        handledElement = null;
        return false;
    }

    private static bool TryDispatchHitRecursive(IVisual el, Point p, Func<IVisual, bool> action, out IVisual? handledElement) {
        if (!el.Visible) {
            handledElement = null;
            return false;
        }

        if (el is IVisualContainer container && container.Children.Count != 0) {
            var children = container.Children
                .OrderByDescending(c => c.Layer)
                .ThenByDescending(c => c.ZIndex);

            foreach (var child in children) {
                if (TryDispatchHitRecursive(child, p, action, out handledElement))
                    return true;
            }
        }

        if (!el.Bounds.Contains(p)) {
            handledElement = null;
            return false;
        }

        try {
            if (!action(el)) {
                handledElement = null;
                return false;
            }

            handledElement = el;
            return true;
        }
        catch {
            handledElement = null;
            return false;
        }

    }

    private IEnumerable<IVisual> EnumerateHitTestRoots() {
        return _roots
            .OrderByDescending(x => x.Layer)
            .ThenByDescending(x => x.ZIndex);
    }

    private static void ClearTrackedElement(ref IVisual? trackedElement, IVisual element) {
        if (ReferenceEquals(trackedElement, element))
            trackedElement = null;
    }

    private bool UpdateHoveredElement(Point p, IVisual? hit) {
        bool handled = false;

        if (_hoveredElement is not null && !ReferenceEquals(_hoveredElement, hit))
            handled = _hoveredElement.OnMouseMove(p);

        if (hit is not null)
            handled = hit.OnMouseMove(p) || handled;

        _hoveredElement = hit;
        return handled;
    }


    public bool MouseDown(Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseDown(p);

        var handled = TryDispatchHit(p, el => el.OnMouseDown(p), out var handledElement);
        if (handled)
            _capturedElement = handledElement;

        return handled;
    }

    public bool MouseUp(Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseUp(p);

        if (_capturedElement is not null) {
            var captured = _capturedElement;
            _capturedElement = null;
            return captured.OnMouseUp(p);
        }

        return TryDispatchHit(p, el => el.OnMouseUp(p), out _);
    }

    public bool MouseMove(Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseMove(p);

        if (_capturedElement is not null) {
            bool capturedHandled = _capturedElement.OnMouseMove(p);
            _hoveredElement = HitTest(p);
            return capturedHandled;
        }

        return UpdateHoveredElement(p, HitTest(p));
    }

    public bool MouseWheel(int delta, Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseWheel(delta, p);

        return TryDispatchHit(p, el => el.OnMouseWheel(delta, p), out _);
    }

    public bool KeyDown(KeyCode key) => FocusedElement?.OnKeyDown(key) ?? false;
    public bool KeyUp(KeyCode key) => FocusedElement?.OnKeyUp(key) ?? false;
    public bool TextInput(string text) => FocusedElement?.OnTextInput(text) ?? false;
    public bool Copy() => FocusedElement?.OnCopy() ?? false;
    public bool Cut() => FocusedElement?.OnCut() ?? false;
    public bool SelectAll() => FocusedElement?.OnSelectAll() ?? false;
    public bool Undo() => FocusedElement?.OnUndo() ?? false;
    public bool Redo() => FocusedElement?.OnRedo() ?? false;
    public bool DeleteWordLeft() => FocusedElement?.DeleteWordLeft() ?? false;
    public bool DeleteWordRight() => FocusedElement?.DeleteWordRight() ?? false;
    public bool PasteFromClipboard() {
        try {
            if (!System.Windows.Forms.Clipboard.ContainsText())
                return false;

            return FocusedElement?.OnPaste(System.Windows.Forms.Clipboard.GetText()) ?? false;
        }
        catch {
            return false;
        }
    }

    public void Dispose() {
        foreach (var el in _elements)
            el.Dispose();

        _elements.Clear();
        _roots.Clear();
    }
}
