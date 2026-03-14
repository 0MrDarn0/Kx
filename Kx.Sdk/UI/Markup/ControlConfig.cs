// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Markup;

public class ControlConfig {
    private readonly HashSet<string> _explicitProperties = [];

    private string _type = string.Empty;
    public string Type {
        get => _type;
        set {
            _type = value;
            MarkExplicit(nameof(Type));
        }
    }

    private string _id = string.Empty;
    public string Id {
        get => _id;
        set {
            _id = value;
            MarkExplicit(nameof(Id));
        }
    }

    private string? _text;
    public string? Text {
        get => _text;
        set {
            _text = value;
            MarkExplicit(nameof(Text));
        }
    }

    private string? _textBinding;
    public string? TextBinding {
        get => _textBinding;
        set {
            _textBinding = value;
            MarkExplicit(nameof(TextBinding));
        }
    }

    private string? _skinKey;
    public string? SkinKey {
        get => _skinKey;
        set {
            _skinKey = value;
            MarkExplicit(nameof(SkinKey));
        }
    }

    private string? _color;
    public string? Color {
        get => _color;
        set {
            _color = value;
            MarkExplicit(nameof(Color));
        }
    }

    private string? _colorBinding;
    public string? ColorBinding {
        get => _colorBinding;
        set {
            _colorBinding = value;
            MarkExplicit(nameof(ColorBinding));
        }
    }

    private string? _fontSizeBinding;
    public string? FontSizeBinding {
        get => _fontSizeBinding;
        set {
            _fontSizeBinding = value;
            MarkExplicit(nameof(FontSizeBinding));
        }
    }

    private BoundsConfig? _bounds;
    public BoundsConfig? Bounds {
        get => _bounds;
        set {
            _bounds = value;
            MarkExplicit(nameof(Bounds));
        }
    }

    private ThicknessConfig? _margin;
    public ThicknessConfig? Margin {
        get => _margin;
        set {
            _margin = value;
            MarkExplicit(nameof(Margin));
        }
    }

    private ThicknessConfig? _padding;
    public ThicknessConfig? Padding {
        get => _padding;
        set {
            _padding = value;
            MarkExplicit(nameof(Padding));
        }
    }

    private string? _dock;
    public string? Dock {
        get => _dock;
        set {
            _dock = value;
            MarkExplicit(nameof(Dock));
        }
    }

    private string _layer = "Content";
    public string Layer {
        get => _layer;
        set {
            _layer = value;
            MarkExplicit(nameof(Layer));
        }
    }

    private string? _visibleBinding;
    public string? VisibleBinding {
        get => _visibleBinding;
        set {
            _visibleBinding = value;
            MarkExplicit(nameof(VisibleBinding));
        }
    }

    private string? _enabledBinding;
    public string? EnabledBinding {
        get => _enabledBinding;
        set {
            _enabledBinding = value;
            MarkExplicit(nameof(EnabledBinding));
        }
    }

    private string? _orientationBinding;
    public string? OrientationBinding {
        get => _orientationBinding;
        set {
            _orientationBinding = value;
            MarkExplicit(nameof(OrientationBinding));
        }
    }

    private string? _spacingBinding;
    public string? SpacingBinding {
        get => _spacingBinding;
        set {
            _spacingBinding = value;
            MarkExplicit(nameof(SpacingBinding));
        }
    }

    private string? _onClick;
    public string? OnClick {
        get => _onClick;
        set {
            _onClick = value;
            MarkExplicit(nameof(OnClick));
        }
    }

    private FontConfig? _font;
    public FontConfig? Font {
        get => _font;
        set {
            _font = value;
            MarkExplicit(nameof(Font));
        }
    }

    private int _gridRow;
    public int GridRow {
        get => _gridRow;
        set {
            _gridRow = value;
            MarkExplicit(nameof(GridRow));
        }
    }

    private int _gridColumn;
    public int GridColumn {
        get => _gridColumn;
        set {
            _gridColumn = value;
            MarkExplicit(nameof(GridColumn));
        }
    }

    private int _gridRowSpan = 1;
    public int GridRowSpan {
        get => _gridRowSpan;
        set {
            _gridRowSpan = value;
            MarkExplicit(nameof(GridRowSpan));
        }
    }

    private int _gridColumnSpan = 1;
    public int GridColumnSpan {
        get => _gridColumnSpan;
        set {
            _gridColumnSpan = value;
            MarkExplicit(nameof(GridColumnSpan));
        }
    }

    private List<GridRowConfig> _rows = [];
    public List<GridRowConfig> Rows {
        get => _rows;
        set {
            _rows = value;
            MarkExplicit(nameof(Rows));
        }
    }

    private List<GridColumnConfig> _columns = [];
    public List<GridColumnConfig> Columns {
        get => _columns;
        set {
            _columns = value;
            MarkExplicit(nameof(Columns));
        }
    }

    private List<ControlConfig> _children = [];
    public List<ControlConfig> Children {
        get => _children;
        set {
            _children = value;
            MarkExplicit(nameof(Children));
        }
    }

    private Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Properties {
        get => _properties;
        set {
            _properties = value;
            MarkExplicit(nameof(Properties));
        }
    }

    public bool IsPropertySet(string propertyName) => _explicitProperties.Contains(propertyName);

    private void MarkExplicit(string propertyName) {
        _explicitProperties.Add(propertyName);
    }
}
