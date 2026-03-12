// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.Markup;

namespace Kx.UI.Markup;

public sealed class ControlRegistry : IControlRegistry {
    private readonly Dictionary<string, ControlBuilder> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string type, ControlBuilder factory) {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(factory);

        _factories[type] = factory;
    }

    public bool TryCreate(IVisualContext context, ControlConfig config, out UIElement? control) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(config);

        control = null;
        if (string.IsNullOrWhiteSpace(config.Type))
            return false;

        if (!_factories.TryGetValue(config.Type, out var factory))
            return false;

        control = factory(context, config);
        return control is not null;
    }
}
