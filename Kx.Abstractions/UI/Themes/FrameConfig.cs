// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI.Themes;

public class FrameConfig {
    private readonly HashSet<string> _explicitProperties = [];

    private FrameStyle _style = FrameStyle.Auto;
    public FrameStyle Style {
        get => _style;
        set {
            _style = value;
            MarkExplicit(nameof(Style));
        }
    }

    private string _topLeft = "KalOnline:Frame:top_left.png";
    public string TopLeft {
        get => _topLeft;
        set {
            _topLeft = value;
            MarkExplicit(nameof(TopLeft));
        }
    }

    private string _topCenter = "KalOnline:Frame:top_center.png";
    public string TopCenter {
        get => _topCenter;
        set {
            _topCenter = value;
            MarkExplicit(nameof(TopCenter));
        }
    }

    private string _topRight = "KalOnline:Frame:top_right.png";
    public string TopRight {
        get => _topRight;
        set {
            _topRight = value;
            MarkExplicit(nameof(TopRight));
        }
    }

    private string _rightCenter = "KalOnline:Frame:right_center.png";
    public string RightCenter {
        get => _rightCenter;
        set {
            _rightCenter = value;
            MarkExplicit(nameof(RightCenter));
        }
    }

    private string _bottomRight = "KalOnline:Frame:bottom_right.png";
    public string BottomRight {
        get => _bottomRight;
        set {
            _bottomRight = value;
            MarkExplicit(nameof(BottomRight));
        }
    }

    private string _bottomCenter = "KalOnline:Frame:bottom_center.png";
    public string BottomCenter {
        get => _bottomCenter;
        set {
            _bottomCenter = value;
            MarkExplicit(nameof(BottomCenter));
        }
    }

    private string _bottomLeft = "KalOnline:Frame:bottom_left.png";
    public string BottomLeft {
        get => _bottomLeft;
        set {
            _bottomLeft = value;
            MarkExplicit(nameof(BottomLeft));
        }
    }

    private string _leftCenter = "KalOnline:Frame:left_center.png";
    public string LeftCenter {
        get => _leftCenter;
        set {
            _leftCenter = value;
            MarkExplicit(nameof(LeftCenter));
        }
    }

    private string _fillBitmap = "KalOnline:Frame:fill_bitmap.bmp";
    public string FillBitmap {
        get => _fillBitmap;
        set {
            _fillBitmap = value;
            MarkExplicit(nameof(FillBitmap));
        }
    }

    private string _fillColor = "#101010";
    public string FillColor {
        get => _fillColor;
        set {
            _fillColor = value;
            MarkExplicit(nameof(FillColor));
        }
    }

    private bool _useFillColor;
    public bool UseFillColor {
        get => _useFillColor;
        set {
            _useFillColor = value;
            MarkExplicit(nameof(UseFillColor));
        }
    }

    private int _topWidthOffset = 7;
    public int TopWidthOffset {
        get => _topWidthOffset;
        set {
            _topWidthOffset = value;
            MarkExplicit(nameof(TopWidthOffset));
        }
    }

    private int _bottomWidthOffset = 15;
    public int BottomWidthOffset {
        get => _bottomWidthOffset;
        set {
            _bottomWidthOffset = value;
            MarkExplicit(nameof(BottomWidthOffset));
        }
    }

    private int _leftHeightOffset = 5;
    public int LeftHeightOffset {
        get => _leftHeightOffset;
        set {
            _leftHeightOffset = value;
            MarkExplicit(nameof(LeftHeightOffset));
        }
    }

    private int _rightHeightOffset = 5;
    public int RightHeightOffset {
        get => _rightHeightOffset;
        set {
            _rightHeightOffset = value;
            MarkExplicit(nameof(RightHeightOffset));
        }
    }

    private int _fillPosOffset = 5;
    public int FillPosOffset {
        get => _fillPosOffset;
        set {
            _fillPosOffset = value;
            MarkExplicit(nameof(FillPosOffset));
        }
    }

    private int _fillWidthOffset = 12;
    public int FillWidthOffset {
        get => _fillWidthOffset;
        set {
            _fillWidthOffset = value;
            MarkExplicit(nameof(FillWidthOffset));
        }
    }

    private int _fillHeightOffset = 9;
    public int FillHeightOffset {
        get => _fillHeightOffset;
        set {
            _fillHeightOffset = value;
            MarkExplicit(nameof(FillHeightOffset));
        }
    }

    private DefaultFrameConfig _default = new();
    public DefaultFrameConfig Default {
        get => _default;
        set {
            _default = value;
            MarkExplicit(nameof(Default));
        }
    }

    public bool IsPropertySet(string propertyName) => _explicitProperties.Contains(propertyName);

    private void MarkExplicit(string propertyName) {
        _explicitProperties.Add(propertyName);
    }
}
