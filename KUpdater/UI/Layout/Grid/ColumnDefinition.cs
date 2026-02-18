// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Layout.Grid;

public class ColumnDefinition {
    public GridLength Width { get; set; } = GridLength.Auto;
    public float ActualWidth { get; internal set; }
}
