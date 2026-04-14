// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Markup;

/// <summary>
/// Registers named window content definitions that can be resolved at runtime.
/// </summary>
public interface IWindowContentRegistry {
    void Register(string name, WindowContentDefinition contentDefinition);
    bool TryGet(string name, out WindowContentDefinition? contentDefinition);
}
