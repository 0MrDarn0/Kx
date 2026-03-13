// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.Themes;

namespace Kx.App;

internal static class WindowDefinitionMerger {
    private static readonly FrameConfig _defaultFrame = new();
    private static readonly DefaultFrameConfig _defaultDefaultFrame = new();
    private static readonly ControlConfig _defaultControl = new();

    public static FrameConfig MergeFrame(FrameConfig windowFrame, WindowTheme? theme) {
        ArgumentNullException.ThrowIfNull(windowFrame);

        var merged = theme is null
            ? CloneFrame(windowFrame)
            : CloneFrame(theme.Frame);

        if (theme is not null)
            ApplyFrameOverrides(merged, windowFrame);

        return merged;
    }

    public static IReadOnlyList<ControlConfig> MergeControls(IReadOnlyList<ControlConfig> windowControls, WindowTheme? theme) {
        ArgumentNullException.ThrowIfNull(windowControls);

        return MergeControlList(theme?.Controls, windowControls);
    }

    private static void ApplyFrameOverrides(FrameConfig target, FrameConfig source) {
        if (source.Style != _defaultFrame.Style)
            target.Style = source.Style;

        if (!string.Equals(source.TopLeft, _defaultFrame.TopLeft, StringComparison.Ordinal))
            target.TopLeft = source.TopLeft;
        if (!string.Equals(source.TopCenter, _defaultFrame.TopCenter, StringComparison.Ordinal))
            target.TopCenter = source.TopCenter;
        if (!string.Equals(source.TopRight, _defaultFrame.TopRight, StringComparison.Ordinal))
            target.TopRight = source.TopRight;
        if (!string.Equals(source.RightCenter, _defaultFrame.RightCenter, StringComparison.Ordinal))
            target.RightCenter = source.RightCenter;
        if (!string.Equals(source.BottomRight, _defaultFrame.BottomRight, StringComparison.Ordinal))
            target.BottomRight = source.BottomRight;
        if (!string.Equals(source.BottomCenter, _defaultFrame.BottomCenter, StringComparison.Ordinal))
            target.BottomCenter = source.BottomCenter;
        if (!string.Equals(source.BottomLeft, _defaultFrame.BottomLeft, StringComparison.Ordinal))
            target.BottomLeft = source.BottomLeft;
        if (!string.Equals(source.LeftCenter, _defaultFrame.LeftCenter, StringComparison.Ordinal))
            target.LeftCenter = source.LeftCenter;
        if (!string.Equals(source.FillBitmap, _defaultFrame.FillBitmap, StringComparison.Ordinal))
            target.FillBitmap = source.FillBitmap;
        if (!string.Equals(source.FillColor, _defaultFrame.FillColor, StringComparison.Ordinal))
            target.FillColor = source.FillColor;
        if (source.UseFillColor != _defaultFrame.UseFillColor)
            target.UseFillColor = source.UseFillColor;
        if (source.TopWidthOffset != _defaultFrame.TopWidthOffset)
            target.TopWidthOffset = source.TopWidthOffset;
        if (source.BottomWidthOffset != _defaultFrame.BottomWidthOffset)
            target.BottomWidthOffset = source.BottomWidthOffset;
        if (source.LeftHeightOffset != _defaultFrame.LeftHeightOffset)
            target.LeftHeightOffset = source.LeftHeightOffset;
        if (source.RightHeightOffset != _defaultFrame.RightHeightOffset)
            target.RightHeightOffset = source.RightHeightOffset;
        if (source.FillPosOffset != _defaultFrame.FillPosOffset)
            target.FillPosOffset = source.FillPosOffset;
        if (source.FillWidthOffset != _defaultFrame.FillWidthOffset)
            target.FillWidthOffset = source.FillWidthOffset;
        if (source.FillHeightOffset != _defaultFrame.FillHeightOffset)
            target.FillHeightOffset = source.FillHeightOffset;

        target.Default = MergeDefaultFrame(target.Default, source.Default);
    }

    private static DefaultFrameConfig MergeDefaultFrame(DefaultFrameConfig themeDefaults, DefaultFrameConfig windowDefaults) {
        var merged = CloneDefaultFrame(themeDefaults);

        if (!string.Equals(windowDefaults.Title, _defaultDefaultFrame.Title, StringComparison.Ordinal))
            merged.Title = windowDefaults.Title;
        if (!string.Equals(windowDefaults.BackgroundColor, _defaultDefaultFrame.BackgroundColor, StringComparison.Ordinal))
            merged.BackgroundColor = windowDefaults.BackgroundColor;
        if (!string.Equals(windowDefaults.TitleBarColor, _defaultDefaultFrame.TitleBarColor, StringComparison.Ordinal))
            merged.TitleBarColor = windowDefaults.TitleBarColor;
        if (!string.Equals(windowDefaults.BorderColor, _defaultDefaultFrame.BorderColor, StringComparison.Ordinal))
            merged.BorderColor = windowDefaults.BorderColor;
        if (!string.Equals(windowDefaults.SeparatorColor, _defaultDefaultFrame.SeparatorColor, StringComparison.Ordinal))
            merged.SeparatorColor = windowDefaults.SeparatorColor;
        if (!string.Equals(windowDefaults.TitleColor, _defaultDefaultFrame.TitleColor, StringComparison.Ordinal))
            merged.TitleColor = windowDefaults.TitleColor;
        if (!string.Equals(windowDefaults.CloseButtonColor, _defaultDefaultFrame.CloseButtonColor, StringComparison.Ordinal))
            merged.CloseButtonColor = windowDefaults.CloseButtonColor;
        if (!string.Equals(windowDefaults.CloseButtonForegroundColor, _defaultDefaultFrame.CloseButtonForegroundColor, StringComparison.Ordinal))
            merged.CloseButtonForegroundColor = windowDefaults.CloseButtonForegroundColor;
        if (windowDefaults.BorderThickness != _defaultDefaultFrame.BorderThickness)
            merged.BorderThickness = windowDefaults.BorderThickness;
        if (windowDefaults.CornerRadius != _defaultDefaultFrame.CornerRadius)
            merged.CornerRadius = windowDefaults.CornerRadius;
        if (windowDefaults.TitleBarHeight != _defaultDefaultFrame.TitleBarHeight)
            merged.TitleBarHeight = windowDefaults.TitleBarHeight;
        if (windowDefaults.TitlePadding != _defaultDefaultFrame.TitlePadding)
            merged.TitlePadding = windowDefaults.TitlePadding;
        if (windowDefaults.TitleFontSize != _defaultDefaultFrame.TitleFontSize)
            merged.TitleFontSize = windowDefaults.TitleFontSize;
        if (windowDefaults.ContentPadding != _defaultDefaultFrame.ContentPadding)
            merged.ContentPadding = windowDefaults.ContentPadding;
        if (windowDefaults.CloseButtonSize != _defaultDefaultFrame.CloseButtonSize)
            merged.CloseButtonSize = windowDefaults.CloseButtonSize;
        if (windowDefaults.CloseButtonMargin != _defaultDefaultFrame.CloseButtonMargin)
            merged.CloseButtonMargin = windowDefaults.CloseButtonMargin;

        return merged;
    }

    private static IReadOnlyList<ControlConfig> MergeControlList(IReadOnlyList<ControlConfig>? themeControls, IReadOnlyList<ControlConfig> windowControls) {
        var merged = themeControls?.Select(CloneControl).ToList() ?? [];
        var indexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < merged.Count; i++) {
            if (!string.IsNullOrWhiteSpace(merged[i].Id))
                indexById[merged[i].Id] = i;
        }

        foreach (var control in windowControls) {
            if (!string.IsNullOrWhiteSpace(control.Id) && indexById.TryGetValue(control.Id, out var index)) {
                merged[index] = MergeControl(merged[index], control);
                continue;
            }

            merged.Add(CloneControl(control));
        }

        return merged;
    }

    private static ControlConfig MergeControl(ControlConfig themeControl, ControlConfig windowControl) {
        var merged = CloneControl(themeControl);

        if (!string.IsNullOrWhiteSpace(windowControl.Type))
            merged.Type = windowControl.Type;
        if (!string.IsNullOrWhiteSpace(windowControl.Id))
            merged.Id = windowControl.Id;
        if (windowControl.Text is not null)
            merged.Text = windowControl.Text;
        if (windowControl.SkinKey is not null)
            merged.SkinKey = windowControl.SkinKey;
        if (windowControl.Color is not null)
            merged.Color = windowControl.Color;
        if (windowControl.Bounds is not null)
            merged.Bounds = CloneBounds(windowControl.Bounds);
        if (windowControl.Margin is not null)
            merged.Margin = CloneThickness(windowControl.Margin);
        if (windowControl.Padding is not null)
            merged.Padding = CloneThickness(windowControl.Padding);
        if (windowControl.Dock is not null)
            merged.Dock = windowControl.Dock;
        if (!string.Equals(windowControl.Layer, _defaultControl.Layer, StringComparison.OrdinalIgnoreCase))
            merged.Layer = windowControl.Layer;
        if (windowControl.OnClick is not null)
            merged.OnClick = windowControl.OnClick;
        if (windowControl.Font is not null)
            merged.Font = CloneFont(windowControl.Font);
        if (windowControl.GridRow != _defaultControl.GridRow)
            merged.GridRow = windowControl.GridRow;
        if (windowControl.GridColumn != _defaultControl.GridColumn)
            merged.GridColumn = windowControl.GridColumn;
        if (windowControl.GridRowSpan != _defaultControl.GridRowSpan)
            merged.GridRowSpan = windowControl.GridRowSpan;
        if (windowControl.GridColumnSpan != _defaultControl.GridColumnSpan)
            merged.GridColumnSpan = windowControl.GridColumnSpan;
        if (windowControl.Rows.Count != 0)
            merged.Rows = windowControl.Rows.Select(CloneGridRow).ToList();
        if (windowControl.Columns.Count != 0)
            merged.Columns = windowControl.Columns.Select(CloneGridColumn).ToList();
        if (windowControl.Children.Count != 0)
            merged.Children = MergeControlList(merged.Children, windowControl.Children).ToList();

        foreach (var property in windowControl.Properties)
            merged.Properties[property.Key] = property.Value;

        return merged;
    }

    private static FrameConfig CloneFrame(FrameConfig source) {
        return new FrameConfig {
            Style = source.Style,
            TopLeft = source.TopLeft,
            TopCenter = source.TopCenter,
            TopRight = source.TopRight,
            RightCenter = source.RightCenter,
            BottomRight = source.BottomRight,
            BottomCenter = source.BottomCenter,
            BottomLeft = source.BottomLeft,
            LeftCenter = source.LeftCenter,
            FillBitmap = source.FillBitmap,
            FillColor = source.FillColor,
            UseFillColor = source.UseFillColor,
            TopWidthOffset = source.TopWidthOffset,
            BottomWidthOffset = source.BottomWidthOffset,
            LeftHeightOffset = source.LeftHeightOffset,
            RightHeightOffset = source.RightHeightOffset,
            FillPosOffset = source.FillPosOffset,
            FillWidthOffset = source.FillWidthOffset,
            FillHeightOffset = source.FillHeightOffset,
            Default = CloneDefaultFrame(source.Default)
        };
    }

    private static DefaultFrameConfig CloneDefaultFrame(DefaultFrameConfig source) {
        return new DefaultFrameConfig {
            Title = source.Title,
            BackgroundColor = source.BackgroundColor,
            TitleBarColor = source.TitleBarColor,
            BorderColor = source.BorderColor,
            SeparatorColor = source.SeparatorColor,
            TitleColor = source.TitleColor,
            CloseButtonColor = source.CloseButtonColor,
            CloseButtonForegroundColor = source.CloseButtonForegroundColor,
            BorderThickness = source.BorderThickness,
            CornerRadius = source.CornerRadius,
            TitleBarHeight = source.TitleBarHeight,
            TitlePadding = source.TitlePadding,
            TitleFontSize = source.TitleFontSize,
            ContentPadding = source.ContentPadding,
            CloseButtonSize = source.CloseButtonSize,
            CloseButtonMargin = source.CloseButtonMargin
        };
    }

    private static ControlConfig CloneControl(ControlConfig source) {
        var clone = new ControlConfig {
            Type = source.Type,
            Id = source.Id,
            Text = source.Text,
            SkinKey = source.SkinKey,
            Color = source.Color,
            Bounds = source.Bounds is null ? null : CloneBounds(source.Bounds),
            Margin = source.Margin is null ? null : CloneThickness(source.Margin),
            Padding = source.Padding is null ? null : CloneThickness(source.Padding),
            Dock = source.Dock,
            Layer = source.Layer,
            OnClick = source.OnClick,
            Font = source.Font is null ? null : CloneFont(source.Font),
            GridRow = source.GridRow,
            GridColumn = source.GridColumn,
            GridRowSpan = source.GridRowSpan,
            GridColumnSpan = source.GridColumnSpan,
            Rows = source.Rows.Select(CloneGridRow).ToList(),
            Columns = source.Columns.Select(CloneGridColumn).ToList(),
            Children = source.Children.Select(CloneControl).ToList(),
            Properties = new Dictionary<string, string>(source.Properties, StringComparer.OrdinalIgnoreCase)
        };

        return clone;
    }

    private static BoundsConfig CloneBounds(BoundsConfig source) {
        return new BoundsConfig {
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height
        };
    }

    private static ThicknessConfig CloneThickness(ThicknessConfig source) {
        return new ThicknessConfig {
            Left = source.Left,
            Top = source.Top,
            Right = source.Right,
            Bottom = source.Bottom
        };
    }

    private static FontConfig CloneFont(FontConfig source) {
        return new FontConfig {
            Name = source.Name,
            Size = source.Size,
            Style = source.Style
        };
    }

    private static GridRowConfig CloneGridRow(GridRowConfig source) {
        return new GridRowConfig {
            Height = CloneGridLength(source.Height)
        };
    }

    private static GridColumnConfig CloneGridColumn(GridColumnConfig source) {
        return new GridColumnConfig {
            Width = CloneGridLength(source.Width)
        };
    }

    private static GridLengthConfig CloneGridLength(GridLengthConfig source) {
        return new GridLengthConfig {
            Value = source.Value,
            Unit = source.Unit
        };
    }
}
