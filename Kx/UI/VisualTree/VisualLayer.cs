// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.UI.VisualTree;

public enum VisualLayer {
    Frame,      // UIElementManager auf dem Rahmen (Titel, Close-Button, Deko)
    Content,    // UIElementManager im Innenbereich (Layout-System)
    Overlay     // Tooltips, Debug, Modal-Overlays
}
