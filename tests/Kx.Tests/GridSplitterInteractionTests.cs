using System.Drawing;

using Kx.Tests.TestInfrastructure;
using Kx.UI.Elements.Panel;

using KxColumnDefinition = Kx.UI.Layout.ColumnDefinition;
using KxGridLength = Kx.UI.Layout.GridLength;
using KxRowDefinition = Kx.UI.Layout.RowDefinition;
using uOrientation = Kx.UI.Layout.Orientation;

namespace Kx.Tests;

public sealed class GridSplitterInteractionTests {
    [Fact]
    public void WhenVerticalSplitterIsDraggedThenAdjacentColumnsResize() {
        var context = new TestVisualContext();
        using var grid = new Grid(context, "layout");
        using var splitter = new GridSplitter(context, "splitter") {
            GridColumn = 1,
            GridRow = 0,
            MinSegmentSize = 40f
        };

        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Pixel(100) });
        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Pixel(8) });
        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Pixel(100) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Pixel(120) });
        grid.AddChild(splitter);
        context.UIElementManager.Add(grid);
        grid.Arrange(new Rectangle(0, 0, 208, 120), 1f);

        Assert.True(context.UIElementManager.MouseDown(new Point(104, 30)));
        Assert.True(context.UIElementManager.MouseMove(new Point(134, 30)));
        Assert.True(context.UIElementManager.MouseUp(new Point(134, 30)));

        grid.Arrange(new Rectangle(0, 0, 208, 120), 1f);

        Assert.Equal(130f, grid.Columns[0].Width.Value);
        Assert.Equal(8f, grid.Columns[1].Width.Value);
        Assert.Equal(70f, grid.Columns[2].Width.Value);
    }

    [Fact]
    public void WhenHorizontalSplitterIsDraggedThenAdjacentRowsResize() {
        var context = new TestVisualContext();
        using var grid = new Grid(context, "layout");
        using var splitter = new GridSplitter(context, "splitter") {
            GridColumn = 0,
            GridRow = 1,
            Orientation = uOrientation.Horizontal,
            MinSegmentSize = 30f
        };

        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Pixel(160) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Pixel(90) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Pixel(8) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Pixel(110) });
        grid.AddChild(splitter);
        context.UIElementManager.Add(grid);
        grid.Arrange(new Rectangle(0, 0, 160, 208), 1f);

        Assert.True(context.UIElementManager.MouseDown(new Point(40, 94)));
        Assert.True(context.UIElementManager.MouseMove(new Point(40, 74)));
        Assert.True(context.UIElementManager.MouseUp(new Point(40, 74)));

        grid.Arrange(new Rectangle(0, 0, 160, 208), 1f);

        Assert.Equal(70f, grid.Rows[0].Height.Value);
        Assert.Equal(8f, grid.Rows[1].Height.Value);
        Assert.Equal(130f, grid.Rows[2].Height.Value);
    }

    [Fact]
    public void WhenVerticalStarColumnsAreDraggedThenLaterWindowResizeKeepsThemResizable() {
        var context = new TestVisualContext();
        using var grid = new Grid(context, "layout");
        using var splitter = new GridSplitter(context, "splitter") {
            GridColumn = 1,
            GridRow = 0,
            MinSegmentSize = 40f
        };

        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Star(1f) });
        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Pixel(8f) });
        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Star(1f) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Pixel(120) });
        grid.AddChild(splitter);
        context.UIElementManager.Add(grid);
        grid.Arrange(new Rectangle(0, 0, 208, 120), 1f);

        Assert.True(context.UIElementManager.MouseDown(new Point(104, 30)));
        Assert.True(context.UIElementManager.MouseMove(new Point(134, 30)));
        Assert.True(context.UIElementManager.MouseUp(new Point(134, 30)));

        grid.Arrange(new Rectangle(0, 0, 308, 120), 1f);

        Assert.Equal(Kx.UI.Layout.GridUnitType.Star, grid.Columns[0].Width.UnitType);
        Assert.Equal(Kx.UI.Layout.GridUnitType.Star, grid.Columns[2].Width.UnitType);
        Assert.InRange(grid.Columns[0].ActualWidth, 194f, 196f);
        Assert.InRange(grid.Columns[2].ActualWidth, 104f, 106f);
    }

    [Fact]
    public void WhenHorizontalStarRowsAreDraggedThenLaterWindowResizeKeepsThemResizable() {
        var context = new TestVisualContext();
        using var grid = new Grid(context, "layout");
        using var splitter = new GridSplitter(context, "splitter") {
            GridColumn = 0,
            GridRow = 1,
            Orientation = uOrientation.Horizontal,
            MinSegmentSize = 30f
        };

        grid.Columns.Add(new KxColumnDefinition { Width = KxGridLength.Pixel(160) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Star(1f) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Pixel(8f) });
        grid.Rows.Add(new KxRowDefinition { Height = KxGridLength.Star(1f) });
        grid.AddChild(splitter);
        context.UIElementManager.Add(grid);
        grid.Arrange(new Rectangle(0, 0, 160, 208), 1f);

        Assert.True(context.UIElementManager.MouseDown(new Point(40, 104)));
        Assert.True(context.UIElementManager.MouseMove(new Point(40, 84)));
        Assert.True(context.UIElementManager.MouseUp(new Point(40, 84)));

        grid.Arrange(new Rectangle(0, 0, 160, 308), 1f);

        Assert.Equal(Kx.UI.Layout.GridUnitType.Star, grid.Rows[0].Height.UnitType);
        Assert.Equal(Kx.UI.Layout.GridUnitType.Star, grid.Rows[2].Height.UnitType);
        Assert.InRange(grid.Rows[0].ActualHeight, 119f, 121f);
        Assert.InRange(grid.Rows[2].ActualHeight, 179f, 181f);
    }
}
