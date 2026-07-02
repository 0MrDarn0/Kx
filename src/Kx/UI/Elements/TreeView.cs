// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;

using SkiaSharp;

namespace Kx.UI.Elements;

public sealed class TreeView : UIElement {
    public sealed class Node {
        public string Name { get; set; }
        public List<Node> Children { get; } = [];
        public bool IsExpanded { get; set; }

        public Node(string name) {
            Name = name;
        }
    }

    private KxColor _textColor = KxColor.Parse("#FFFFFF");
    private KxColor _backgroundColor = new(16, 16, 16);
    private KxColor _selectedColor = new(124, 110, 75, 180);
    private SKFont? _font;
    private float _fontSize = 12f;

    private readonly List<Node> _roots = [];
    private readonly List<(Node node, int level)> _visible = [];
    private int _selectedIndex = -1;
    private int _hoveredIndex = -1;

    public event Action<Node?>? SelectedNodeChanged;

    public TreeView(IVisualContext ctx, string id) : base(ctx, id) {
        Padding = new Kx.Sdk.UI.Layout.Thickness(4);
        UpdateFont();
    }

    public void SetNodes(IEnumerable<Node>? nodes) {
        _roots.Clear();
        if (nodes is not null)
            _roots.AddRange(nodes);
        _selectedIndex = -1;
        RebuildVisible();
        Invalidate();
    }

    public Node? SelectedNode => _selectedIndex >= 0 && _selectedIndex < _visible.Count ? _visible[_selectedIndex].node : null;

    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
        UpdateFont();
    }

    private void UpdateFont() {
        _font?.Dispose();
        _font = new SKFont(SKTypeface.Default, _fontSize * DpiScale);
    }

    public override void Measure(float dpi) {
        if (FixedBounds is Rectangle fb) {
            DesiredSize = new Size(fb.Width + (int)(Margin.Horizontal * dpi), fb.Height + (int)(Margin.Vertical * dpi));
            return;
        }

        DesiredSize = new Size((int)(240 * dpi), (int)(260 * dpi));
    }

    protected override void OnDraw(IKxCanvas canvas) {
        if (_font is null || !Visible)
            return;

        var rect = LayoutRect;
        var content = ContentRect;
        canvas.DrawRect(rect.Left, rect.Top, rect.Right, rect.Bottom, _backgroundColor);

        int itemHeight = GetItemHeight();
        int y = content.Top;

        for (int i = 0; i < _visible.Count; i++) {
            var (node, level) = _visible[i];
            var itemRect = new Rectangle(content.Left, y, content.Width, itemHeight);

            if (i == _selectedIndex)
                canvas.DrawRect(itemRect.Left, itemRect.Top, itemRect.Right, itemRect.Bottom, _selectedColor);

            // expand/collapse icon
            float iconX = itemRect.Left + level * 12 + 4;
            float iconY = itemRect.Top + (itemHeight - 8f) / 2f;
            if (node.Children.Count > 0) {
                float iconSize = 8f;
                float stroke = Math.Max(1f, DpiScale);
                if (node.IsExpanded) {
                    canvas.DrawLine(iconX, iconY + 2f, iconX + (iconSize / 2f), iconY + iconSize - 1f, _textColor, stroke);
                    canvas.DrawLine(iconX + iconSize, iconY + 2f, iconX + (iconSize / 2f), iconY + iconSize - 1f, _textColor, stroke);
                } else {
                    canvas.DrawLine(iconX + 2f, iconY, iconX + iconSize - 1f, iconY + (iconSize / 2f), _textColor, stroke);
                    canvas.DrawLine(iconX + 2f, iconY + iconSize, iconX + iconSize - 1f, iconY + (iconSize / 2f), _textColor, stroke);
                }
            }

            float textX = itemRect.Left + level * 12 + 16;
            float baseline = itemRect.Top - _font.Metrics.Ascent + 2;
            canvas.DrawText(node.Name, textX, baseline, _font.Size, _textColor);

            y += itemHeight;
        }
    }

    public override bool OnMouseDown(Point point) {
        if (!Bounds.Contains(point))
            return false;

        int idx = GetIndexAt(point);
        if (idx < 0)
            return false;

        var (node, level) = _visible[idx];
        // check icon area
        var content = ContentRect;
        int itemHeight = GetItemHeight();
        int y = content.Top + idx * itemHeight;
        var iconRect = new Rectangle(content.Left + level * 12 + 4, y + (itemHeight - 8) / 2, 8, 8);
        if (node.Children.Count > 0 && iconRect.Contains(point)) {
            node.IsExpanded = !node.IsExpanded;
            RebuildVisible();
            Invalidate();
            return true;
        }

        SetSelectedIndex(idx);
        return true;
    }

    public override bool OnMouseMove(Point point) {
        int idx = GetIndexAt(point);
        if (idx == _hoveredIndex)
            return idx >= 0;

        _hoveredIndex = idx;
        Invalidate();
        return true;
    }

    private void SetSelectedIndex(int idx) {
        if (idx == _selectedIndex)
            return;

        _selectedIndex = idx;
        Invalidate();
        SelectedNodeChanged?.Invoke(SelectedNode);
    }

    private int GetIndexAt(Point p) {
        var content = ContentRect;
        if (!content.Contains(p))
            return -1;

        int itemHeight = GetItemHeight();
        int relY = p.Y - content.Top;
        int idx = relY / itemHeight;
        return idx >= 0 && idx < _visible.Count ? idx : -1;
    }

    private int GetItemHeight() {
        if (_font is null)
            return 20;

        var metrics = _font.Metrics;
        return Math.Max(20, (int)Math.Ceiling((metrics.Descent - metrics.Ascent) + 6));
    }

    private void RebuildVisible() {
        _visible.Clear();
        foreach (var root in _roots)
            AddVisibleRecursive(root, 0);
    }

    private void AddVisibleRecursive(Node node, int level) {
        _visible.Add((node, level));
        if (node.IsExpanded) {
            foreach (var c in node.Children)
                AddVisibleRecursive(c, level + 1);
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _font?.Dispose();
        }

        base.Dispose(disposing);
    }
}
