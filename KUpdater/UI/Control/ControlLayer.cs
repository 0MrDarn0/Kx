// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Control;

public enum ControlLayer {
    Frame,      // Controls auf dem Rahmen (Titel, Close-Button, Deko)
    Content,    // Controls im Innenbereich (Layout-System)
    Overlay     // Tooltips, Debug, Modal-Overlays
}
