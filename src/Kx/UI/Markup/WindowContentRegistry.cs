// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Markup;

namespace Kx.UI.Markup;

public sealed class WindowContentRegistry : IWindowContentRegistry {
    private readonly Dictionary<string, WindowContentDefinition> _contentDefinitions = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, WindowContentDefinition contentDefinition) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(contentDefinition);

        _contentDefinitions[name] = contentDefinition;
    }

    public bool TryGet(string name, out WindowContentDefinition? contentDefinition) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _contentDefinitions.TryGetValue(name, out contentDefinition);
    }
}
