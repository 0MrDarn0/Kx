// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace Kx.UI.State;

internal static class UiStateValueConverter {
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
