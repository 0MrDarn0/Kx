// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Layout;

public enum GridUnitType {
    Auto,
    Pixel,
    Star
}

public struct GridLength {
    public float Value { get; }
    public GridUnitType UnitType { get; }

    public GridLength(float value, GridUnitType type) {
        Value = value;
        UnitType = type;
    }

    public static GridLength Auto => new GridLength(0, GridUnitType.Auto);
    public static GridLength Star(float value = 1) => new GridLength(value, GridUnitType.Star);
    public static GridLength Pixel(float value) => new GridLength(value, GridUnitType.Pixel);
}
