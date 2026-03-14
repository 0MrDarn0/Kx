// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace Kx.Sdk.UI.Layout;

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
    public static SKColor BoundsColor { get; set; } = new(255, 0, 0, 100);
    public static SKColor LayoutColor { get; set; } = new(0, 0, 255, 100);
    public static SKColor TextBgColor { get; set; } = new(0, 0, 0, 180);
    public static SKColor TextColor { get; set; } = SKColors.White;

    /// <summary>
    /// Toggles the given overlay type.
    /// </summary>
    /// <param name="type">The overlay type to toggle.</param>
    public static void Toggle(OverlayType type) {
        static bool Get(OverlayType t) => t switch {
            OverlayType.Bounds => ShowBounds,
            OverlayType.LayoutRect => ShowLayoutRect,
            OverlayType.Meta => ShowMeta,
            OverlayType.ParentChain => ShowParentChain,
            OverlayType.ContentRect => ShowOnlyHoveredElement,
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
                case OverlayType.ContentRect:
                    ShowOnlyHoveredElement = value;
                    break;
            }
        }

        if (!Enabled) {
            Set(type, true);
            Enabled = true;
            return;
        }

        if (Get(type)) {
            Set(type, false);
            if (!ShowBounds && !ShowLayoutRect && !ShowMeta && !ShowParentChain && !ShowOnlyHoveredElement)
                Enabled = false;
            return;
        }

        ShowBounds = false;
        ShowLayoutRect = false;
        ShowMeta = false;
        ShowParentChain = false;
        ShowOnlyHoveredElement = false;

        Set(type, true);
        Enabled = true;
    }

    /// <summary>
    /// Gets whether the given overlay type is currently enabled.
    /// </summary>
    /// <param name="type">The overlay type to inspect.</param>
    public static bool IsOn(OverlayType type) => type switch {
        OverlayType.Bounds => ShowBounds,
        OverlayType.LayoutRect => ShowLayoutRect,
        OverlayType.Meta => ShowMeta,
        OverlayType.ParentChain => ShowParentChain,
        _ => false
    };
}
