// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Markup;

/// <summary>
/// Describes the value type of a control property exposed to tooling.
/// </summary>
public enum ControlPropertyKind {
    String,
    Integer,
    Float,
    Boolean,
    Color,
    Bounds,
    Thickness,
    Dictionary,
    Enum
}

/// <summary>
/// Describes where a control property is stored in markup.
/// </summary>
public enum ControlPropertySource {
    TopLevel,
    PropertiesBag
}

/// <summary>
/// Describes a control property that can be shown and edited by tooling.
/// </summary>
public sealed class ControlPropertyDescriptor {
    /// <summary>
    /// Initializes a new property descriptor.
    /// </summary>
    /// <param name="name">The display and logical name of the property.</param>
    /// <param name="kind">The value kind used for editor selection and parsing.</param>
    /// <param name="markupKey">The key used in markup, when different from <paramref name="name"/>.</param>
    /// <param name="source">The markup source where the property is persisted.</param>
    /// <param name="isCommon">Indicates whether the property belongs to the shared base set across control types.</param>
    public ControlPropertyDescriptor(string name, ControlPropertyKind kind, string? markupKey = null, ControlPropertySource source = ControlPropertySource.TopLevel, bool isCommon = false) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Kind = kind;
        MarkupKey = string.IsNullOrWhiteSpace(markupKey) ? null : markupKey;
        Source = source;
        IsCommon = isCommon;
    }

    /// <summary>
    /// Gets the display and logical name of the property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value kind used for editor selection and parsing.
    /// </summary>
    public ControlPropertyKind Kind { get; }

    /// <summary>
    /// Gets the key used in markup.
    /// </summary>
    public string? MarkupKey { get; }

    /// <summary>
    /// Gets the markup source where the property is persisted.
    /// </summary>
    public ControlPropertySource Source { get; }

    /// <summary>
    /// Gets a value indicating whether the property belongs to the shared base set.
    /// </summary>
    public bool IsCommon { get; }
}
