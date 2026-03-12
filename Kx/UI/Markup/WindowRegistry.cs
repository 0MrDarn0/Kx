// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.UI.Markup;

public sealed class WindowRegistry : IWindowRegistry {
    private readonly Dictionary<string, WindowConfig> _windows = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, WindowConfig config) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(config);

        _windows[name] = config;
    }

    public bool TryGet(string name, out WindowConfig? config) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _windows.TryGetValue(name, out config);
    }
}
