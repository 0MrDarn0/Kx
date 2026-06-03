// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.Markup;

namespace Kx.UI.Markup;

public sealed class ControlRegistry : IControlRegistry {
    private readonly Dictionary<string, ControlBuilder> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<ControlPropertyDescriptor>> _properties = new(StringComparer.OrdinalIgnoreCase);

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

    public void RegisterProperties(string type, IEnumerable<ControlPropertyDescriptor> properties) {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(properties);

        _properties[type] = properties.ToArray();
    }

    public IReadOnlyList<ControlPropertyDescriptor> GetProperties(string type) {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return _properties.TryGetValue(type, out var properties)
            ? properties
            : [];
    }
}
