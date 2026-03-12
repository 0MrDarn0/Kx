// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI;
using Kx.UI.Elements;

namespace Kx.UI.Markup;

public delegate UIElement ControlBuilder(IVisualContext context, ControlConfig config);

/// <summary>
/// Registers and creates UI controls by markup type name.
/// </summary>
public interface IControlRegistry {
    /// <summary>
    /// Registers a factory for the specified control type.
    /// </summary>
    /// <param name="type">The unique markup type name.</param>
    /// <param name="factory">The factory used to create the control.</param>
    void Register(string type, ControlBuilder factory);

    /// <summary>
    /// Tries to create a control for the specified markup definition.
    /// </summary>
    /// <param name="context">The visual context used to create the control.</param>
    /// <param name="config">The markup definition for the control.</param>
    /// <param name="control">The created control, when successful.</param>
    /// <returns><see langword="true"/> when a matching control factory was found.</returns>
    bool TryCreate(IVisualContext context, ControlConfig config, out UIElement? control);
}
