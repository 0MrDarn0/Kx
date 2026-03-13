// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Drawing;

using Kx.Abstractions.UI.VisualTree;

namespace Kx.Abstractions.UI;

public interface IUIElementManager {
    bool MouseMove(Point location);
    bool MouseDown(Point location);
    bool MouseUp(Point location);
    bool MouseWheel(int delta, Point location);
    void Add(IVisual element);
    void Remove(IVisual element);
    void SetFocus(IVisual element);
    void ClearFocus();
    bool TryGet(string id, out IVisual? visual);
    void Dispose();
}
