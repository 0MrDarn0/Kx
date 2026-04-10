// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Themes;

/// <summary>
/// Registers named window frame definitions that can be applied by windows and plugins.
/// </summary>
public interface IWindowFrameRegistry {
    void Register(string name, WindowFrameDefinition frameDefinition);
    bool TryGet(string name, out WindowFrameDefinition? frameDefinition);
}
