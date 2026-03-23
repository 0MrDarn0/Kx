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

    public enum OverlayPreset {
        Off,
        Layout,
        Hierarchy,
        Inspect
    }

    public static bool Enabled { get; set; } = false;
    public static bool ShowBounds { get; set; } = false;
    public static bool ShowLayoutRect { get; set; } = false;
    public static bool ShowMeta { get; set; } = false;
    public static bool ShowParentChain { get; set; } = false;
    public static bool ShowContentRect { get; set; } = false;
    public static bool ShowOnlyHoveredElement { get; set; } = false;
    public static float FontSize { get; set; } = 12f;
    public static float ItemSpacing { get; set; } = 4f;
    public static float ItemPadding { get; set; } = 4f;
    public static int MaxParentItems { get; set; } = 20;
    public static SKColor BoundsColor { get; set; } = new(255, 0, 0, 100);
    public static SKColor LayoutColor { get; set; } = new(0, 0, 255, 100);
    public static SKColor ContentColor { get; set; } = new(0, 255, 0, 120);
    public static SKColor TextBgColor { get; set; } = new(0, 0, 0, 180);
    public static SKColor TextColor { get; set; } = SKColors.White;

    /// <summary>
    /// Applies a named debug overlay preset.
    /// </summary>
    /// <param name="preset">The preset to enable.</param>
    public static void ApplyPreset(OverlayPreset preset) {
        switch (preset) {
            case OverlayPreset.Off:
                SetFlags(showBounds: false, showLayoutRect: false, showMeta: false, showParentChain: false, showContentRect: false, showOnlyHoveredElement: false);
                Enabled = false;
                break;

            case OverlayPreset.Layout:
                SetFlags(showBounds: true, showLayoutRect: true, showMeta: false, showParentChain: false, showContentRect: true, showOnlyHoveredElement: false);
                Enabled = true;
                break;

            case OverlayPreset.Hierarchy:
                SetFlags(showBounds: false, showLayoutRect: false, showMeta: true, showParentChain: true, showContentRect: false, showOnlyHoveredElement: true);
                Enabled = true;
                break;

            case OverlayPreset.Inspect:
                SetFlags(showBounds: true, showLayoutRect: true, showMeta: true, showParentChain: true, showContentRect: true, showOnlyHoveredElement: true);
                Enabled = true;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
        }
    }

    /// <summary>
    /// Cycles through the available debug overlay presets.
    /// </summary>
    public static void CyclePreset() {
        ApplyPreset(GetCurrentPreset() switch {
            OverlayPreset.Off => OverlayPreset.Layout,
            OverlayPreset.Layout => OverlayPreset.Hierarchy,
            OverlayPreset.Hierarchy => OverlayPreset.Inspect,
            OverlayPreset.Inspect => OverlayPreset.Off,
            _ => OverlayPreset.Layout
        });
    }

    /// <summary>
    /// Gets the active preset when the current flags match one exactly.
    /// </summary>
    /// <returns>The active preset, or <see langword="null"/> for a custom flag combination.</returns>
    public static OverlayPreset? GetCurrentPreset() {
        if (!Enabled && !ShowBounds && !ShowLayoutRect && !ShowMeta && !ShowParentChain && !ShowContentRect && !ShowOnlyHoveredElement)
            return OverlayPreset.Off;

        if (Enabled && ShowBounds && ShowLayoutRect && !ShowMeta && !ShowParentChain && ShowContentRect && !ShowOnlyHoveredElement)
            return OverlayPreset.Layout;

        if (Enabled && !ShowBounds && !ShowLayoutRect && ShowMeta && ShowParentChain && !ShowContentRect && ShowOnlyHoveredElement)
            return OverlayPreset.Hierarchy;

        if (Enabled && ShowBounds && ShowLayoutRect && ShowMeta && ShowParentChain && ShowContentRect && ShowOnlyHoveredElement)
            return OverlayPreset.Inspect;

        return null;
    }

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
            OverlayType.ContentRect => ShowContentRect,
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
                    ShowContentRect = value;
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
            if (!ShowBounds && !ShowLayoutRect && !ShowMeta && !ShowParentChain && !ShowContentRect)
                Enabled = false;
            return;
        }

        ShowBounds = false;
        ShowLayoutRect = false;
        ShowMeta = false;
        ShowParentChain = false;
        ShowContentRect = false;

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
        OverlayType.ContentRect => ShowContentRect,
        _ => false
    };

    private static void SetFlags(bool showBounds, bool showLayoutRect, bool showMeta, bool showParentChain, bool showContentRect, bool showOnlyHoveredElement) {
        ShowBounds = showBounds;
        ShowLayoutRect = showLayoutRect;
        ShowMeta = showMeta;
        ShowParentChain = showParentChain;
        ShowContentRect = showContentRect;
        ShowOnlyHoveredElement = showOnlyHoveredElement;
    }
}
