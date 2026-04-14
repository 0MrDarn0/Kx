// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Localization;

/// <summary>
/// Represents a strongly typed localization key.
/// </summary>
public readonly record struct LanguageKey {
    /// <summary>
    /// Initializes a new localization key.
    /// </summary>
    /// <param name="value">The dotted localization id.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
    public LanguageKey(string value) {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("The localization key must not be null or whitespace.", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Gets the dotted localization id.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Converts the key to its dotted localization id.
    /// </summary>
    public override string ToString() => Value;

    public static implicit operator LanguageKey(string value) => new(value);
}
