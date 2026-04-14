// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Sdk.Events;

using SkiaSharp;

namespace Kx.Sdk.UI.VisualTree;

public interface IVisual : IDisposable {
    string Id { get; }
    Rectangle Bounds { get; }
    bool UseContentArea { get; set; }
    VisualLayer Layer { get; set; }
    bool Visible { get; set; }
    int ZIndex { get; set; }
    void OnDpiChanged(float scale);
    bool CanFocus { get; }
    bool IsFocused { get; }
    void OnFocusGained();
    void OnFocusLost();
    bool OnKeyDown(KeyCode key);
    bool OnKeyUp(KeyCode key);
    void Draw(SKCanvas canvas);
    bool OnMouseMove(Point p);
    bool OnMouseDown(Point p);
    bool OnMouseUp(Point p);
    bool OnMouseWheel(int delta, Point p);
    void Measure(float dpiScale);
    void Arrange(Rectangle bounds, float dpiScale);
    bool OnTextInput(string text);
    bool OnCopy();
    bool OnCut();
    bool OnPaste(string text);
    bool OnSelectAll();
    bool OnUndo();
    bool OnRedo();
    bool DeleteWordLeft();
    bool DeleteWordRight();
}
