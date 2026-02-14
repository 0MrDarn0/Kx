// Copyright (c) 2026 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Utility;

public static class MakeColor {
    // 🔹 Hex → Color (#RRGGBB oder #AARRGGBB)
    public static Color FromHex(string hex) {
        return ColorTranslator.FromHtml(hex);
    }

    // 🔹 Color → Hex (#RRGGBB)
    public static string ToHex(Color color) {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // 🔹 Color → Hex mit Alpha (#AARRGGBB)
    public static string ToHexWithAlpha(Color color) {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // 🔹 RGB → Color
    public static Color FromRgb(int r, int g, int b) {
        return Color.FromArgb(r, g, b);
    }

    // 🔹 RGBA → Color
    public static Color FromRgba(int a, int r, int g, int b) {
        return Color.FromArgb(a, r, g, b);
    }

    // 🔹 Color → RGB Tuple
    public static (int R, int G, int B) ToRgb(Color color) {
        return (color.R, color.G, color.B);
    }

    // 🔹 Color → RGBA Tuple
    public static (int A, int R, int G, int B) ToRgba(Color color) {
        return (color.A, color.R, color.G, color.B);
    }
}
