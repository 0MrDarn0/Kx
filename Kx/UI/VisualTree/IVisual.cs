// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace Kx.UI.VisualTree;

public interface IVisual : IDisposable {
    string Id { get; }
    VisualLayer Layer { get; }
    bool Visible { get; set; }
    int ZIndex { get; set; }
    void OnDpiChanged(float scale);
    bool CanFocus { get; }
    bool IsFocused { get; }
    void OnFocusGained();
    void OnFocusLost();
    bool OnKeyDown(Keys key);
    bool OnKeyUp(Keys key);
    void Draw(SKCanvas canvas);
    bool OnMouseMove(Point p);
    bool OnMouseDown(Point p);
    bool OnMouseUp(Point p);
    bool OnMouseWheel(int delta, Point p);
    void Measure(float dpiScale);
    void Arrange(Rectangle bounds, float dpiScale);
}
