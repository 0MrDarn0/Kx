// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Binding;

/// <summary>
/// Represents a parsed markup binding expression.
/// </summary>
public sealed record UiBindingExpression(string Path, IReadOnlyList<UiBindingConverter> Converters) {
    public static bool TryParse(string? expression, out UiBindingExpression? binding) {
        binding = null;
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        string[] segments = expression.Split('|', StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || string.IsNullOrWhiteSpace(segments[0]))
            return false;

        string path = segments[0];
        if (segments.Length == 1) {
            binding = new UiBindingExpression(path, []);
            return true;
        }

        var converters = new List<UiBindingConverter>(segments.Length - 1);
        for (int i = 1; i < segments.Length; i++) {
            if (!UiBindingConverter.TryParse(segments[i], out var converter) || converter is null)
                return false;

            converters.Add(converter);
        }

        binding = new UiBindingExpression(path, converters);
        return true;
    }
}

/// <summary>
/// Represents one converter segment in a markup binding expression.
/// </summary>
public sealed record UiBindingConverter(string Name, string? Argument = null) {
    public static bool TryParse(string? expression, out UiBindingConverter? converter) {
        converter = null;
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        int separatorIndex = expression.IndexOf(':');
        if (separatorIndex < 0) {
            converter = new UiBindingConverter(expression.Trim());
            return true;
        }

        string name = expression[..separatorIndex].Trim();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        string argument = expression[(separatorIndex + 1)..].Trim();
        converter = new UiBindingConverter(name, argument);
        return true;
    }
}
