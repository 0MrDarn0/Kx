// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;

namespace Kx.App;

internal static class WindowCompositionMerger {
    public static FrameConfig MergeFrame(FrameConfig windowFrame, WindowFrameDefinition? frameDefinition) {
        ArgumentNullException.ThrowIfNull(windowFrame);

        var merged = frameDefinition is null
            ? CloneFrame(windowFrame)
            : CloneFrame(frameDefinition.Frame);

        if (frameDefinition is not null)
            ApplyFrameOverrides(merged, windowFrame);

        return merged;
    }

    public static IReadOnlyList<ControlConfig> MergeControls(IReadOnlyList<ControlConfig> windowControls, WindowFrameDefinition? frameDefinition) {
        ArgumentNullException.ThrowIfNull(windowControls);

        return MergeControlList(frameDefinition?.Controls, windowControls);
    }

    private static void ApplyFrameOverrides(FrameConfig target, FrameConfig source) {
        if (source.IsPropertySet(nameof(FrameConfig.Style)))
            target.Style = source.Style;

        if (source.IsPropertySet(nameof(FrameConfig.TopLeft)))
            target.TopLeft = source.TopLeft;
        if (source.IsPropertySet(nameof(FrameConfig.TopCenter)))
            target.TopCenter = source.TopCenter;
        if (source.IsPropertySet(nameof(FrameConfig.TopRight)))
            target.TopRight = source.TopRight;
        if (source.IsPropertySet(nameof(FrameConfig.RightCenter)))
            target.RightCenter = source.RightCenter;
        if (source.IsPropertySet(nameof(FrameConfig.BottomRight)))
            target.BottomRight = source.BottomRight;
        if (source.IsPropertySet(nameof(FrameConfig.BottomCenter)))
            target.BottomCenter = source.BottomCenter;
        if (source.IsPropertySet(nameof(FrameConfig.BottomLeft)))
            target.BottomLeft = source.BottomLeft;
        if (source.IsPropertySet(nameof(FrameConfig.LeftCenter)))
            target.LeftCenter = source.LeftCenter;
        if (source.IsPropertySet(nameof(FrameConfig.FillBitmap)))
            target.FillBitmap = source.FillBitmap;
        if (source.IsPropertySet(nameof(FrameConfig.FillColor)))
            target.FillColor = source.FillColor;
        if (source.IsPropertySet(nameof(FrameConfig.UseFillColor)))
            target.UseFillColor = source.UseFillColor;
        if (source.IsPropertySet(nameof(FrameConfig.TopWidthOffset)))
            target.TopWidthOffset = source.TopWidthOffset;
        if (source.IsPropertySet(nameof(FrameConfig.BottomWidthOffset)))
            target.BottomWidthOffset = source.BottomWidthOffset;
        if (source.IsPropertySet(nameof(FrameConfig.LeftHeightOffset)))
            target.LeftHeightOffset = source.LeftHeightOffset;
        if (source.IsPropertySet(nameof(FrameConfig.RightHeightOffset)))
            target.RightHeightOffset = source.RightHeightOffset;
        if (source.IsPropertySet(nameof(FrameConfig.FillPosOffset)))
            target.FillPosOffset = source.FillPosOffset;
        if (source.IsPropertySet(nameof(FrameConfig.FillWidthOffset)))
            target.FillWidthOffset = source.FillWidthOffset;
        if (source.IsPropertySet(nameof(FrameConfig.FillHeightOffset)))
            target.FillHeightOffset = source.FillHeightOffset;

        target.Default = MergeDefaultFrame(target.Default, source.Default);
    }

    private static DefaultFrameConfig MergeDefaultFrame(DefaultFrameConfig frameDefaults, DefaultFrameConfig windowDefaults) {
        var merged = CloneDefaultFrame(frameDefaults);

        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.Title)))
            merged.Title = windowDefaults.Title;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.Icon)))
            merged.Icon = windowDefaults.Icon;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.BackgroundColor)))
            merged.BackgroundColor = windowDefaults.BackgroundColor;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.TitleBarColor)))
            merged.TitleBarColor = windowDefaults.TitleBarColor;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.BorderColor)))
            merged.BorderColor = windowDefaults.BorderColor;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.SeparatorColor)))
            merged.SeparatorColor = windowDefaults.SeparatorColor;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.TitleColor)))
            merged.TitleColor = windowDefaults.TitleColor;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.CloseButtonColor)))
            merged.CloseButtonColor = windowDefaults.CloseButtonColor;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.CloseButtonForegroundColor)))
            merged.CloseButtonForegroundColor = windowDefaults.CloseButtonForegroundColor;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.BorderThickness)))
            merged.BorderThickness = windowDefaults.BorderThickness;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.CornerRadius)))
            merged.CornerRadius = windowDefaults.CornerRadius;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.TitleBarHeight)))
            merged.TitleBarHeight = windowDefaults.TitleBarHeight;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.TitlePadding)))
            merged.TitlePadding = windowDefaults.TitlePadding;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.TitleFontSize)))
            merged.TitleFontSize = windowDefaults.TitleFontSize;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.ContentPadding)))
            merged.ContentPadding = windowDefaults.ContentPadding;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.CloseButtonSize)))
            merged.CloseButtonSize = windowDefaults.CloseButtonSize;
        if (windowDefaults.IsPropertySet(nameof(DefaultFrameConfig.CloseButtonMargin)))
            merged.CloseButtonMargin = windowDefaults.CloseButtonMargin;

        return merged;
    }

    private static IReadOnlyList<ControlConfig> MergeControlList(IReadOnlyList<ControlConfig>? frameControls, IReadOnlyList<ControlConfig> windowControls) {
        var merged = frameControls?.Select(CloneControl).ToList() ?? [];
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

    private static ControlConfig MergeControl(ControlConfig frameControl, ControlConfig windowControl) {
        ArgumentNullException.ThrowIfNull(frameControl);
        ArgumentNullException.ThrowIfNull(windowControl);

        var merged = CloneControl(frameControl);

        if (windowControl.IsPropertySet(nameof(ControlConfig.Type)))
            merged.Type = windowControl.Type;
        if (windowControl.IsPropertySet(nameof(ControlConfig.Id)))
            merged.Id = windowControl.Id;
        if (windowControl.IsPropertySet(nameof(ControlConfig.Text)))
            merged.Text = windowControl.Text;
        if (windowControl.IsPropertySet(nameof(ControlConfig.TextBinding)))
            merged.TextBinding = windowControl.TextBinding;
        if (windowControl.IsPropertySet(nameof(ControlConfig.NormalImage)))
            merged.NormalImage = windowControl.NormalImage;
        if (windowControl.IsPropertySet(nameof(ControlConfig.HoverImage)))
            merged.HoverImage = windowControl.HoverImage;
        if (windowControl.IsPropertySet(nameof(ControlConfig.PressedImage)))
            merged.PressedImage = windowControl.PressedImage;
        if (windowControl.IsPropertySet(nameof(ControlConfig.Color)))
            merged.Color = windowControl.Color;
        if (windowControl.IsPropertySet(nameof(ControlConfig.ColorBinding)))
            merged.ColorBinding = windowControl.ColorBinding;
        if (windowControl.IsPropertySet(nameof(ControlConfig.FontSizeBinding)))
            merged.FontSizeBinding = windowControl.FontSizeBinding;
        if (windowControl.IsPropertySet(nameof(ControlConfig.Bounds)))
            merged.Bounds = windowControl.Bounds is null ? null : CloneBounds(windowControl.Bounds);
        if (windowControl.IsPropertySet(nameof(ControlConfig.Margin)))
            merged.Margin = windowControl.Margin is null ? null : CloneThickness(windowControl.Margin);
        if (windowControl.IsPropertySet(nameof(ControlConfig.Padding)))
            merged.Padding = windowControl.Padding is null ? null : CloneThickness(windowControl.Padding);
        if (windowControl.IsPropertySet(nameof(ControlConfig.Dock)))
            merged.Dock = windowControl.Dock;
        if (windowControl.IsPropertySet(nameof(ControlConfig.Layer)))
            merged.Layer = windowControl.Layer;
        if (windowControl.IsPropertySet(nameof(ControlConfig.VisibleBinding)))
            merged.VisibleBinding = windowControl.VisibleBinding;
        if (windowControl.IsPropertySet(nameof(ControlConfig.EnabledBinding)))
            merged.EnabledBinding = windowControl.EnabledBinding;
        if (windowControl.IsPropertySet(nameof(ControlConfig.OrientationBinding)))
            merged.OrientationBinding = windowControl.OrientationBinding;
        if (windowControl.IsPropertySet(nameof(ControlConfig.SpacingBinding)))
            merged.SpacingBinding = windowControl.SpacingBinding;
        if (windowControl.IsPropertySet(nameof(ControlConfig.OnClick)))
            merged.OnClick = windowControl.OnClick;
        if (windowControl.IsPropertySet(nameof(ControlConfig.Font)))
            merged.Font = windowControl.Font is null ? null : CloneFont(windowControl.Font);
        if (windowControl.IsPropertySet(nameof(ControlConfig.GridRow)))
            merged.GridRow = windowControl.GridRow;
        if (windowControl.IsPropertySet(nameof(ControlConfig.GridColumn)))
            merged.GridColumn = windowControl.GridColumn;
        if (windowControl.IsPropertySet(nameof(ControlConfig.GridRowSpan)))
            merged.GridRowSpan = windowControl.GridRowSpan;
        if (windowControl.IsPropertySet(nameof(ControlConfig.GridColumnSpan)))
            merged.GridColumnSpan = windowControl.GridColumnSpan;
        if (windowControl.IsPropertySet(nameof(ControlConfig.Rows)))
            merged.Rows = windowControl.Rows?.Select(CloneGridRow).ToList() ?? [];
        if (windowControl.IsPropertySet(nameof(ControlConfig.Columns)))
            merged.Columns = windowControl.Columns?.Select(CloneGridColumn).ToList() ?? [];
        if (windowControl.IsPropertySet(nameof(ControlConfig.Children)))
            merged.Children = MergeControlList(merged.Children, windowControl.Children ?? []).ToList();

        if (windowControl.IsPropertySet(nameof(ControlConfig.Properties))) {
            foreach (var property in windowControl.Properties ?? [])
                merged.Properties[property.Key] = property.Value;
        }

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
            Icon = source.Icon,
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
        ArgumentNullException.ThrowIfNull(source);

        var clone = new ControlConfig {
            Type = source.Type,
            Id = source.Id,
            Text = source.Text,
            TextBinding = source.TextBinding,
            NormalImage = source.NormalImage,
            HoverImage = source.HoverImage,
            PressedImage = source.PressedImage,
            Color = source.Color,
            ColorBinding = source.ColorBinding,
            FontSizeBinding = source.FontSizeBinding,
            Bounds = source.Bounds is null ? null : CloneBounds(source.Bounds),
            Margin = source.Margin is null ? null : CloneThickness(source.Margin),
            Padding = source.Padding is null ? null : CloneThickness(source.Padding),
            Dock = source.Dock,
            Layer = source.Layer,
            VisibleBinding = source.VisibleBinding,
            EnabledBinding = source.EnabledBinding,
            OrientationBinding = source.OrientationBinding,
            SpacingBinding = source.SpacingBinding,
            OnClick = source.OnClick,
            Font = source.Font is null ? null : CloneFont(source.Font),
            GridRow = source.GridRow,
            GridColumn = source.GridColumn,
            GridRowSpan = source.GridRowSpan,
            GridColumnSpan = source.GridColumnSpan,
            Rows = source.Rows?.Select(CloneGridRow).ToList() ?? [],
            Columns = source.Columns?.Select(CloneGridColumn).ToList() ?? [],
            Children = source.Children?.Select(CloneControl).ToList() ?? []
        };

        foreach (var property in source.Properties ?? [])
            clone.Properties[property.Key] = property.Value;

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
            Unit = source.Unit,
            Value = source.Value
        };
    }
}
