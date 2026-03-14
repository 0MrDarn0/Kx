// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;
using Kx.Abstractions.UI.Binding;

namespace Kx.UI.State;

internal static class UiStateValueConverter {
    public static bool TryApplyBindingConverters(object? value, IReadOnlyList<UiBindingConverter> converters, out object? convertedValue) {
        convertedValue = value;

        foreach (var converter in converters) {
            if (!TryApplyBindingConverter(convertedValue, converter, out convertedValue))
                return false;
        }

        return true;
    }

    private static bool TryApplyBindingConverter(object? value, UiBindingConverter converter, out object? convertedValue) {
        switch (converter.Name.Trim().ToLowerInvariant()) {
            case "upper":
            case "uppercase":
                if (!TryGetText(value, out var upperText)) {
                    convertedValue = null;
                    return false;
                }

                convertedValue = upperText.ToUpperInvariant();
                return true;

            case "lower":
            case "lowercase":
                if (!TryGetText(value, out var lowerText)) {
                    convertedValue = null;
                    return false;
                }

                convertedValue = lowerText.ToLowerInvariant();
                return true;

            case "trim":
                if (!TryGetText(value, out var trimmedText)) {
                    convertedValue = null;
                    return false;
                }

                convertedValue = trimmedText.Trim();
                return true;

            case "not":
                if (!TryGetBool(value, out var boolValue)) {
                    convertedValue = null;
                    return false;
                }

                convertedValue = !boolValue;
                return true;

            case "default":
                if (value is null || value is string stringValue && string.IsNullOrWhiteSpace(stringValue)) {
                    convertedValue = converter.Argument ?? string.Empty;
                    return true;
                }

                convertedValue = value;
                return true;

            case "prefix":
                if (!TryGetText(value, out var prefixedText)) {
                    convertedValue = null;
                    return false;
                }

                convertedValue = (converter.Argument ?? string.Empty) + prefixedText;
                return true;

            case "suffix":
                if (!TryGetText(value, out var suffixedText)) {
                    convertedValue = null;
                    return false;
                }

                convertedValue = suffixedText + (converter.Argument ?? string.Empty);
                return true;

            case "equals":
                if (!TryGetText(value, out var textValue)) {
                    convertedValue = null;
                    return false;
                }

                convertedValue = string.Equals(textValue, converter.Argument ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                return true;

            default:
                convertedValue = null;
                return false;
        }
    }

    public static bool TryGetText(object? value, out string text) {
        if (value is string stringValue) {
            text = stringValue;
            return true;
        }

        if (value is not null) {
            text = value.ToString() ?? string.Empty;
            return true;
        }

        text = string.Empty;
        return false;
    }

    public static bool TryGetBool(object? value, out bool result) {
        switch (value) {
            case bool boolValue:
                result = boolValue;
                return true;

            case string stringValue when bool.TryParse(stringValue, out var parsed):
                result = parsed;
                return true;

            default:
                result = false;
                return false;
        }
    }

    public static bool TryGetFloat(object? value, out float result) {
        switch (value) {
            case float floatValue:
                result = floatValue;
                return true;

            case double doubleValue:
                result = (float)doubleValue;
                return true;

            case int intValue:
                result = intValue;
                return true;

            case string stringValue when float.TryParse(stringValue, out var parsed):
                result = parsed;
                return true;

            default:
                result = 0f;
                return false;
        }
    }

    public static bool TryGetOrientation(object? value, out Layout.Orientation orientation) {
        switch (value) {
            case Layout.Orientation typedOrientation:
                orientation = typedOrientation;
                return true;

            case string stringValue when Enum.TryParse<Layout.Orientation>(stringValue, ignoreCase: true, out var parsed):
                orientation = parsed;
                return true;

            default:
                orientation = default;
                return false;
        }
    }

    public static bool TryGetColor(object? value, out SKColor color) {
        switch (value) {
            case SKColor skColor:
                color = skColor;
                return true;

            case string stringValue when !string.IsNullOrWhiteSpace(stringValue):
                color = SKColor.Parse(stringValue);
                return true;

            default:
                color = default;
                return false;
        }
    }
}
