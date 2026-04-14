// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Themes;

namespace Kx.UI.Themes;

public sealed class WindowFrameRegistry : IWindowFrameRegistry {
    private readonly Dictionary<string, WindowFrameDefinition> _frameDefinitions = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, WindowFrameDefinition frameDefinition) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(frameDefinition);

        _frameDefinitions[name] = frameDefinition;
    }

    public bool TryGet(string name, out WindowFrameDefinition? frameDefinition) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _frameDefinitions.TryGetValue(name, out frameDefinition);
    }
}
