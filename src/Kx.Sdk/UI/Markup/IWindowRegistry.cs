// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Markup;

/// <summary>
/// Registers named window definitions that can be resolved at runtime.
/// </summary>
public interface IWindowRegistry {
    void Register(string name, WindowConfig config);
    bool TryGet(string name, out WindowConfig? config);
}
