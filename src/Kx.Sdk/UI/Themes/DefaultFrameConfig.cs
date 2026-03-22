// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Themes;

public class DefaultFrameConfig {
    private readonly HashSet<string> _explicitProperties = [];

    private string _title = string.Empty;
    public string Title {
        get => _title;
        set {
            _title = value;
            MarkExplicit(nameof(Title));
        }
    }

    private string? _icon;
    public string? Icon {
        get => _icon;
        set {
            _icon = value;
            MarkExplicit(nameof(Icon));
        }
    }

    private string _backgroundColor = "#1E1F22";
    public string BackgroundColor {
        get => _backgroundColor;
        set {
            _backgroundColor = value;
            MarkExplicit(nameof(BackgroundColor));
        }
    }

    private string _titleBarColor = "#25262B";
    public string TitleBarColor {
        get => _titleBarColor;
        set {
            _titleBarColor = value;
            MarkExplicit(nameof(TitleBarColor));
        }
    }

    private string _borderColor = "#3A3D46";
    public string BorderColor {
        get => _borderColor;
        set {
            _borderColor = value;
            MarkExplicit(nameof(BorderColor));
        }
    }

    private string _separatorColor = "#3A3D46";
    public string SeparatorColor {
        get => _separatorColor;
        set {
            _separatorColor = value;
            MarkExplicit(nameof(SeparatorColor));
        }
    }

    private string _titleColor = "#F5F5F5";
    public string TitleColor {
        get => _titleColor;
        set {
            _titleColor = value;
            MarkExplicit(nameof(TitleColor));
        }
    }

    private string _closeButtonColor = "#2D2F36";
    public string CloseButtonColor {
        get => _closeButtonColor;
        set {
            _closeButtonColor = value;
            MarkExplicit(nameof(CloseButtonColor));
        }
    }

    private string _closeButtonForegroundColor = "#F5F5F5";
    public string CloseButtonForegroundColor {
        get => _closeButtonForegroundColor;
        set {
            _closeButtonForegroundColor = value;
            MarkExplicit(nameof(CloseButtonForegroundColor));
        }
    }

    private int _borderThickness = 1;
    public int BorderThickness {
        get => _borderThickness;
        set {
            _borderThickness = value;
            MarkExplicit(nameof(BorderThickness));
        }
    }

    private int _cornerRadius = 10;
    public int CornerRadius {
        get => _cornerRadius;
        set {
            _cornerRadius = value;
            MarkExplicit(nameof(CornerRadius));
        }
    }

    private int _titleBarHeight = 36;
    public int TitleBarHeight {
        get => _titleBarHeight;
        set {
            _titleBarHeight = value;
            MarkExplicit(nameof(TitleBarHeight));
        }
    }

    private int _titlePadding = 14;
    public int TitlePadding {
        get => _titlePadding;
        set {
            _titlePadding = value;
            MarkExplicit(nameof(TitlePadding));
        }
    }

    private int _titleFontSize = 14;
    public int TitleFontSize {
        get => _titleFontSize;
        set {
            _titleFontSize = value;
            MarkExplicit(nameof(TitleFontSize));
        }
    }

    private int _contentPadding = 10;
    public int ContentPadding {
        get => _contentPadding;
        set {
            _contentPadding = value;
            MarkExplicit(nameof(ContentPadding));
        }
    }

    private int _closeButtonSize = 24;
    public int CloseButtonSize {
        get => _closeButtonSize;
        set {
            _closeButtonSize = value;
            MarkExplicit(nameof(CloseButtonSize));
        }
    }

    private int _closeButtonMargin = 6;
    public int CloseButtonMargin {
        get => _closeButtonMargin;
        set {
            _closeButtonMargin = value;
            MarkExplicit(nameof(CloseButtonMargin));
        }
    }

    public bool IsPropertySet(string propertyName) => _explicitProperties.Contains(propertyName);

    private void MarkExplicit(string propertyName) {
        _explicitProperties.Add(propertyName);
    }
}
