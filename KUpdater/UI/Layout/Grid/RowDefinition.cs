// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Layout.Grid;

public class RowDefinition {
    public GridLength Height { get; set; } = GridLength.Auto;
    public float ActualHeight { get; internal set; }
}
