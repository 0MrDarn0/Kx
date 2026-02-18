// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace KUpdater.UI.Control;

public interface IControl : IDisposable {
    string Id { get; }
    ControlLayer Layer { get; }
    bool Visible { get; set; }
    Rectangle Bounds { get; }

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
}
