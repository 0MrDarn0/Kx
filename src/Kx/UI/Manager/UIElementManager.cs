// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Events;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.VisualTree;

using SkiaSharp;

namespace Kx.UI.Manager;

/// <summary>
/// Manages the visual tree of UI elements, including layout, hit testing,
/// focus, modal overlays, and input dispatch.
/// </summary>
public class UIElementManager : IUIElementManager, IDisposable {

    // ---------------------------------------------------------------------
    // Fields
    // ---------------------------------------------------------------------

    private readonly List<IVisual> _elements = [];
    private readonly List<IVisual> _roots = [];
    private readonly ReaderWriterLockSlim _lock = new();

    private IVisual? _hoveredElement;
    private IVisual? _capturedElement;

    private readonly Comparison<IVisual> _sortComparison = (a, b) => {
        int layer = a.Layer.CompareTo(b.Layer);
        return layer != 0
            ? layer
            : a.ZIndex.CompareTo(b.ZIndex);
    };

    // ---------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------

    /// <summary>
    /// Gets all registered visuals (including roots and children).
    /// </summary>
    public IEnumerable<IVisual> Elements => _elements;

    /// <summary>
    /// Gets all root visuals (top-level elements).
    /// </summary>
    public IEnumerable<IVisual> Roots => _roots;

    /// <summary>
    /// Gets the currently hovered visual, if any.
    /// </summary>
    public IVisual? HoveredElement => _hoveredElement;

    /// <summary>
    /// Gets the currently focused visual, if any.
    /// </summary>
    public IVisual? FocusedElement { get; private set; }

    /// <summary>
    /// Gets the currently active modal visual, if any.
    /// When set, only this visual receives hit testing and mouse input.
    /// </summary>
    public IVisual? ModalElement { get; private set; }

    /// <summary>
    /// Gets the current DPI scale used for layout and rendering.
    /// </summary>
    public float DpiScale { get; private set; } = 1f;

    // ---------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------

    /// <summary>
    /// Returns a thread-safe snapshot of all registered visuals.
    /// </summary>
    public IReadOnlyList<IVisual> GetElementsSnapshot() {
        _lock.EnterReadLock();
        try {
            return [.. _elements];
        }
        finally {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Adds a visual to the manager and, if applicable, to the root collection.
    /// </summary>
    public void Add(IVisual el) {
        if (el == null)
            return;

        _lock.EnterWriteLock();
        try {
            _elements.Add(el);

            if (el is not UIElement { Parent: not null } && !_roots.Contains(el))
                _roots.Add(el);

            SortElements();
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Tries to find a visual by its <see cref="IVisual.Id"/> in the visual tree.
    /// </summary>
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

    /// <summary>
    /// Adds a visual as a root element, optionally attaching it to the element list.
    /// </summary>
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

    /// <summary>
    /// Removes a visual from the manager and disposes it.
    /// </summary>
    public void Remove(IVisual el) {
        if (el == null)
            return;

        ClearTrackedElement(ref _hoveredElement, el);
        ClearTrackedElement(ref _capturedElement, el);

        _elements.Remove(el);
        _roots.Remove(el);

        el.Dispose();
    }

    /// <summary>
    /// Arranges all root elements within the given content and window rectangles.
    /// </summary>
    /// <param name="contentRect">Client area inside the window frame.</param>
    /// <param name="windowRect">Full window area for overlays/popups.</param>
    public void LayoutAll(SKRect contentRect, SKRect windowRect) {
        var dpi = DpiScale;

        foreach (var root in _roots)
            root.Measure(dpi);

        foreach (var root in _roots) {
            var target = root.UseContentArea ? contentRect.ToRectangle() : windowRect.ToRectangle();
            root.Arrange(target, dpi);
        }
    }

    /// <summary>
    /// Brings the specified visual to the front by adjusting its Z-index.
    /// </summary>
    public void BringToFront(IVisual el) {
        if (el == null)
            return;

        el.ZIndex = _elements.Count != 0 ? _elements.Max(x => x.ZIndex) + 1 : 0;
        SortElements();
    }

    /// <summary>
    /// Sends the specified visual to the back by adjusting its Z-index.
    /// </summary>
    public void SendToBack(IVisual el) {
        if (el == null)
            return;

        el.ZIndex = _elements.Count != 0 ? _elements.Min(x => x.ZIndex) - 1 : 0;
        SortElements();
    }

    /// <summary>
    /// Sets keyboard focus to the specified visual.
    /// </summary>
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

    /// <summary>
    /// Clears the current keyboard focus, if any.
    /// </summary>
    public void ClearFocus() {
        var previous = FocusedElement;

        if (FocusedElement is Visual oldVisual)
            oldVisual.SetFocused(false);

        previous?.OnFocusLost();

        FocusedElement = null;
    }

    /// <summary>
    /// Shows a visual as a modal element and brings it to front.
    /// </summary>
    public void ShowModal(IVisual el) {
        ModalElement = el;
        BringToFront(el);
    }

    /// <summary>
    /// Closes the currently active modal element, if any.
    /// </summary>
    public void CloseModal() {
        ModalElement = null;
    }

    /// <summary>
    /// Updates the DPI scale and notifies all visuals.
    /// </summary>
    public void SetDpiScale(float scale) {
        DpiScale = scale;

        foreach (var el in _elements)
            if (el is Visual visual)
                visual.SetDpiScale(scale);
            else
                el.OnDpiChanged(scale);
    }

    /// <summary>
    /// Renders all visible visuals to the given canvas.
    /// </summary>
    public void Render(SKCanvas canvas) {
        foreach (var el in _elements)
            if (el.Visible)
                el.Draw(canvas);
    }

    // ---------------------------------------------------------------------
    // Hit testing
    // ---------------------------------------------------------------------

    /// <summary>
    /// Performs hit testing at the given point, respecting modal elements.
    /// </summary>
    private IVisual? HitTest(Point p) {
        if (ModalElement != null && ModalElement.Visible) {
            if (ModalElement.Bounds.Contains(p))
                return ModalElement;
            return null;
        }

        foreach (var root in EnumerateHitTestRoots()) {
            var hit = HitTestRecursive(root, p);
            if (hit is not null)
                return hit;
        }

        return null;
    }

    /// <summary>
    /// Recursively searches for a visual with the given id starting at the specified root.
    /// </summary>
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

    /// <summary>
    /// Recursively performs hit testing within the visual tree.
    /// Children are tested from top to bottom (Z-order).
    /// </summary>
    private static IVisual? HitTestRecursive(IVisual el, Point p) {
        if (!el.Visible)
            return null;

        if (el is IVisualContainer container && container.Children.Count != 0) {
            var children = container.Children;
            for (var i = children.Count - 1; i >= 0; i--) {
                var child = children[i];
                var hit = HitTestRecursive(child, p);
                if (hit is not null)
                    return hit;
            }
        }

        return el.Bounds.Contains(p) ? el : null;
    }

    /// <summary>
    /// Tries to dispatch an input action to the visual under the given point.
    /// </summary>
    private bool TryDispatchHit(Point p, Func<IVisual, bool> action, out IVisual? handledElement) {
        foreach (var root in EnumerateHitTestRoots()) {
            if (TryDispatchHitRecursive(root, p, action, out handledElement))
                return true;
        }

        handledElement = null;
        return false;
    }

    /// <summary>
    /// Recursively dispatches an input action to the first visual that handles it.
    /// </summary>
    private static bool TryDispatchHitRecursive(IVisual el, Point p, Func<IVisual, bool> action, out IVisual? handledElement) {
        handledElement = null;

        if (!el.Visible)
            return false;

        if (el is IVisualContainer container && container.Children.Count != 0) {
            var children = container.Children;
            for (var i = children.Count - 1; i >= 0; i--) {
                var child = children[i];
                if (TryDispatchHitRecursive(child, p, action, out handledElement))
                    return true;
            }
        }

        if (!el.Bounds.Contains(p))
            return false;

        try {
            if (!action(el))
                return false;

            handledElement = el;
            return true;
        }
        catch {
            return false;
        }
    }

    /// <summary>
    /// Enumerates root visuals in hit-test order (topmost first).
    /// </summary>
    private IEnumerable<IVisual> EnumerateHitTestRoots() {
        return _roots
            .OrderByDescending(x => x.Layer)
            .ThenByDescending(x => x.ZIndex);
    }

    /// <summary>
    /// Clears a tracked element reference if it matches the specified element.
    /// </summary>
    private static void ClearTrackedElement(ref IVisual? trackedElement, IVisual element) {
        if (ReferenceEquals(trackedElement, element))
            trackedElement = null;
    }

    /// <summary>
    /// Updates the hovered element based on the current hit test result.
    /// </summary>
    private bool UpdateHoveredElement(Point p, IVisual? hit) {
        bool handled = false;

        if (_hoveredElement is not null && !ReferenceEquals(_hoveredElement, hit))
            handled = _hoveredElement.OnMouseMove(p);

        if (hit is not null)
            handled = hit.OnMouseMove(p) || handled;

        _hoveredElement = hit;
        return handled;
    }

    // ---------------------------------------------------------------------
    // Mouse input
    // ---------------------------------------------------------------------

    /// <summary>
    /// Handles mouse down at the given point, including capture logic.
    /// </summary>
    public bool MouseDown(Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseDown(p);

        var handled = TryDispatchHit(p, el => el.OnMouseDown(p), out var handledElement);
        if (handled)
            _capturedElement = handledElement;

        return handled;
    }

    /// <summary>
    /// Handles mouse up at the given point, releasing capture if set.
    /// </summary>
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

    /// <summary>
    /// Handles mouse move at the given point, including hover and capture behavior.
    /// </summary>
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

    /// <summary>
    /// Handles mouse wheel input at the given point.
    /// </summary>
    public bool MouseWheel(int delta, Point p) {
        if (ModalElement != null)
            return ModalElement.OnMouseWheel(delta, p);

        return TryDispatchHit(p, el => el.OnMouseWheel(delta, p), out _);
    }

    // ---------------------------------------------------------------------
    // Keyboard / text input
    // ---------------------------------------------------------------------

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

    /// <summary>
    /// Attempts to paste text from the system clipboard into the focused element.
    /// </summary>
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

    // ---------------------------------------------------------------------
    // Disposal
    // ---------------------------------------------------------------------

    /// <summary>
    /// Disposes all managed visuals and releases internal resources.
    /// </summary>
    public void Dispose() {
        _lock.EnterWriteLock();
        try {
            foreach (var el in _elements)
                el.Dispose();

            _elements.Clear();
            _roots.Clear();
        }
        finally {
            _lock.ExitWriteLock();
            _lock.Dispose();
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Sorts elements and roots by layer and Z-index.
    /// </summary>
    private void SortElements() {
        _elements.Sort(_sortComparison);
        _roots.Sort(_sortComparison);
    }
}
