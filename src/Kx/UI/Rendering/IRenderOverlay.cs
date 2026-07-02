// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.UI.Rendering;

internal interface IRenderOverlay {
    string Id { get; }

    void Draw(RenderOverlayContext context);
}
