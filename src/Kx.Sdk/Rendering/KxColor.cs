// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Globalization;

namespace Kx.Sdk.Rendering;

/// <summary>
/// Represents an immutable RGBA color value independent of any rendering backend.
/// </summary>
public readonly record struct KxColor(byte R, byte G, byte B, byte A = 255) {
    /// <summary>
    /// Creates a color value from ARGB channel values.
    /// </summary>
    /// <param name="a">The alpha channel.</param>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <returns>A color containing the specified channels.</returns>
    public static KxColor FromArgb(byte a, byte r, byte g, byte b) => new(r, g, b, a);

    /// <summary>
    /// Creates a color value from an RGB hex string with optional alpha channel.
    /// </summary>
    /// <param name="hex">The hex value in #RRGGBB or #AARRGGBB format.</param>
    /// <returns>A parsed color value.</returns>
    /// <exception cref="FormatException">Thrown when the input is not a supported hex color value.</exception>
    public static KxColor Parse(string hex) {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);

        var value = hex.Trim();
        if (value.StartsWith("#", StringComparison.Ordinal))
            value = value[1..];

        return value.Length switch {
            6 => new KxColor(
                ParseChannel(value.AsSpan(0, 2)),
                ParseChannel(value.AsSpan(2, 2)),
                ParseChannel(value.AsSpan(4, 2))),
            8 => new KxColor(
                ParseChannel(value.AsSpan(2, 2)),
                ParseChannel(value.AsSpan(4, 2)),
                ParseChannel(value.AsSpan(6, 2)),
                ParseChannel(value.AsSpan(0, 2))),
            _ => throw new FormatException("Color value must use #RRGGBB or #AARRGGBB format.")
        };
    }

    /// <summary>
    /// Converts the current color to an uppercase #AARRGGBB hex representation.
    /// </summary>
    /// <returns>The color as #AARRGGBB.</returns>
    public string ToHex() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    private static byte ParseChannel(ReadOnlySpan<char> value) {
        if (!byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var channel))
            throw new FormatException("Color contains invalid hex channel values.");

        return channel;
    }
}
