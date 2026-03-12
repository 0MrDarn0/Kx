// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI.Themes;

public class DefaultFrameConfig {
    public string Title { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#1E1F22";
    public string TitleBarColor { get; set; } = "#25262B";
    public string BorderColor { get; set; } = "#3A3D46";
    public string SeparatorColor { get; set; } = "#3A3D46";
    public string TitleColor { get; set; } = "#F5F5F5";
    public string CloseButtonColor { get; set; } = "#2D2F36";
    public string CloseButtonForegroundColor { get; set; } = "#F5F5F5";
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; } = 10;
    public int TitleBarHeight { get; set; } = 36;
    public int TitlePadding { get; set; } = 14;
    public int TitleFontSize { get; set; } = 14;
    public int ContentPadding { get; set; } = 10;
    public int CloseButtonSize { get; set; } = 24;
    public int CloseButtonMargin { get; set; } = 6;
}
