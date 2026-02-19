// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

public static partial class DebugOverlay {
    public enum OverlayType {
        Bounds,
        LayoutRect,
        Meta,
        ParentChain,
        ContentRect,
        PerfOverlay,
        GridOverlay
    }
    public static bool Enabled { get; set; } = false;
    public static bool ShowBounds { get; set; } = false;
    public static bool ShowLayoutRect { get; set; } = false;
    public static bool ShowMeta { get; set; } = false;
    public static bool ShowParentChain { get; set; } = false;
    public static bool ShowOnlyHoveredElement { get; set; } = false;
    public static float FontSize { get; set; } = 12f;
    public static float ItemSpacing { get; set; } = 4f;
    public static float ItemPadding { get; set; } = 4f;
    public static int MaxParentItems { get; set; } = 20;
    public static SKColor BoundsColor { get; set; } = new SKColor(255, 0, 0, 100);
    public static SKColor LayoutColor { get; set; } = new SKColor(0, 0, 255, 100);
    public static SKColor TextBgColor { get; set; } = new SKColor(0, 0, 0, 180);
    public static SKColor TextColor { get; set; } = SKColors.White;

    /// <summary>
    /// Toggle the given overlay type.
    /// Behavior:
    /// - If Enabled == false: turn the requested flag on and set Enabled = true.
    /// - If Enabled == true and the requested flag is already on: turn it off; if no flags remain on, set Enabled = false.
    /// - If Enabled == true and the requested flag is off: turn all other flags off and turn the requested flag on (exclusive switch).
    /// </summary>
    public static void Toggle(OverlayType type) {
        // Helper local functions to get/set flags by type
        static bool Get(OverlayType t) => t switch {
            OverlayType.Bounds => ShowBounds,
            OverlayType.LayoutRect => ShowLayoutRect,
            OverlayType.Meta => ShowMeta,
            OverlayType.ParentChain => ShowParentChain,
            _ => false
        };

        static void Set(OverlayType t, bool value) {
            switch (t) {
                case OverlayType.Bounds:
                ShowBounds = value;
                break;
                case OverlayType.LayoutRect:
                ShowLayoutRect = value;
                break;
                case OverlayType.Meta:
                ShowMeta = value;
                break;
                case OverlayType.ParentChain:
                ShowParentChain = value;
                break;
            }
        }

        // All overlay types (expand if you add more)
        var allTypes = new[] { OverlayType.Bounds, OverlayType.LayoutRect, OverlayType.Meta, OverlayType.ParentChain };

        // Current state of requested flag
        var current = Get(type);

        if (!Enabled) {
            // If overlay globally disabled, enable requested flag and global Enabled
            Set(type, true);
            Enabled = true;
            return;
        }

        // If enabled and requested flag already on -> turn it off
        if (current) {
            Set(type, false);

            // If no flags remain true, disable global Enabled
            bool anyOn = false;
            foreach (var t in allTypes) {
                if (Get(t)) { anyOn = true; break; }
            }

            if (!anyOn)
                Enabled = false;

            return;
        }

        // Enabled == true and requested flag is off -> make it exclusive:
        // turn off all others, turn this one on
        foreach (var t in allTypes)
            Set(t, false);

        Set(type, true);
        Enabled = true;
    }

    /// <summary>
    /// Convenience: get current value for a type (useful for menu checked state).
    /// </summary>
    public static bool IsOn(OverlayType type) => type switch {
        OverlayType.Bounds => ShowBounds,
        OverlayType.LayoutRect => ShowLayoutRect,
        OverlayType.Meta => ShowMeta,
        OverlayType.ParentChain => ShowParentChain,
        _ => false
    };
}
