// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.UI.VisualTree;

public interface IVisualContainer : IVisual {
    IReadOnlyList<IVisual> Children { get; }
}
