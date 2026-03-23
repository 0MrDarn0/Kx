using Kx.Sdk.UI.Markup;
using Kx.UI.Actions;
using Kx.UI.Elements.Panel;
using Kx.UI.Markup;
using Kx.Tests.TestInfrastructure;

using SkiaSharp;

namespace Kx.Tests;

public sealed class ControlFactoryBindingTests {
    [Fact]
    public void WhenLabelTextBindingUsesConverterPipelineThenConvertedTextIsApplied() {
        var context = new TestVisualContext();
        context.State.Set("example.title", "  plugin window  ");
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "Label",
            Id = "title",
            TextBinding = "example.title|trim|upper|prefix:[BOUND] "
        });

        var label = Assert.IsType<Kx.UI.Elements.Label>(control);
        Assert.Equal("[BOUND]PLUGIN WINDOW", label.Text.Value);
    }

    [Fact]
    public void WhenBoundStateChangesThenLabelTextUpdates() {
        var context = new TestVisualContext();
        context.State.Set("example.title", "initial");
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "Label",
            Id = "title",
            TextBinding = "example.title|upper"
        });

        context.State.Set("example.title", "updated");

        var label = Assert.IsType<Kx.UI.Elements.Label>(control);
        Assert.Equal("UPDATED", label.Text.Value);
    }

    [Fact]
    public void WhenVisibilityBindingUsesNotConverterThenVisibilityIsInverted() {
        var context = new TestVisualContext();
        context.State.Set("example.visible", true);
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "Label",
            Id = "hint",
            VisibleBinding = "example.visible|not"
        });

        Assert.False(control.Visible);
    }

    [Fact]
    public void WhenStackPanelSpacingBindingChangesThenSpacingUpdates() {
        var context = new TestVisualContext();
        context.State.Set("example.spacing", 4f);
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "StackPanel",
            Id = "panel",
            SpacingBinding = "example.spacing"
        });

        context.State.Set("example.spacing", 12f);

        var panel = Assert.IsType<StackPanel>(control);
        Assert.Equal(12f, panel.Spacing);
    }

    [Fact]
    public void WhenTextBoxTextBindingChangesThenTextUpdates() {
        var context = new TestVisualContext();
        context.State.Set("updater.changelog", "Initial");
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "TextBox",
            Id = "changelog",
            TextBinding = "updater.changelog"
        });

        context.State.Set("updater.changelog", "Updated");

        var textBox = Assert.IsType<Kx.UI.Elements.TextBox>(control);
        Assert.Equal("Updated", textBox.Text);
    }

    [Fact]
    public void WhenProgressBarBindingUsesPercentThenProgressIsNormalized() {
        var context = new TestVisualContext();
        context.State.Set("updater.progress", 25);
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "ProgressBar",
            Id = "progress",
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["progressBinding"] = "updater.progress"
            }
        });

        var progressBar = Assert.IsType<Kx.UI.Elements.ProgressBar>(control);
        Assert.Equal(0.25f, progressBar.Progress);
    }

    [Fact]
    public void WhenListBoxBindingsChangeThenItemsAndSelectionUpdate() {
        var context = new TestVisualContext();
        context.State.Set("updater.news.items", new[] { "First", "Second" });
        context.State.Set("updater.news.selectedIndex", 1);
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "ListBox",
            Id = "news",
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["itemsBinding"] = "updater.news.items",
                ["selectedIndexBinding"] = "updater.news.selectedIndex"
            }
        });

        var listBox = Assert.IsType<Kx.UI.Elements.ListBox>(control);
        Assert.Equal(2, listBox.Items.Count);
        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void WhenListBoxVisualPropertiesAreConfiguredThenTheyAreApplied() {
        var context = new TestVisualContext();
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "ListBox",
            Id = "news",
            Color = "#F5F5F5",
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["backgroundColor"] = "#101010",
                ["borderColor"] = "#7C6E4B",
                ["borderThickness"] = "3",
                ["scrollBarColor"] = "#7C6E4B",
                ["glowEnabled"] = "true",
                ["glowColor"] = "#FFFFFF",
                ["glowRadius"] = "6",
                ["selectedItemColor"] = "#6F613F",
                ["selectedItemBorderColor"] = "#E8D9B4",
                ["hoveredItemColor"] = "#2F2A1D",
                ["separatorColor"] = "#5C5238"
            }
        });

        var listBox = Assert.IsType<Kx.UI.Elements.ListBox>(control);
        Assert.True(listBox.GlowEnabled);
        Assert.Equal(SKColor.Parse("#7C6E4B"), listBox.ScrollBarColor);
        Assert.Equal(SKColor.Parse("#E8D9B4"), listBox.SelectedItemBorderColor);
    }

    [Fact]
    public void WhenVisualOffsetPropertiesAreConfiguredThenControlLayoutIsShifted() {
        var context = new TestVisualContext();
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "Label",
            Id = "title",
            Text = "Title",
            Bounds = new BoundsConfig {
                X = 10,
                Y = 0,
                Width = 100,
                Height = 20
            },
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["visualOffsetY"] = "-8"
            }
        });

        control.Arrange(new Rectangle(0, 0, 400, 200), 1f);

        Assert.Equal(-8, control.LayoutRect.Y);
    }

    [Fact]
    public void WhenButtonColorIsConfiguredThenForegroundColorIsApplied() {
        var context = new TestVisualContext();
        var registry = CreateControlRegistry();
        var actions = new MarkupActionRegistry();

        var control = ControlFactory.Create(registry, actions, context, new ControlConfig {
            Type = "Button",
            Id = "start",
            Color = "#E8D9B4"
        });

        var button = Assert.IsType<Kx.UI.Elements.Button>(control);
        Assert.Equal(SKColor.Parse("#E8D9B4"), button.ForegroundColor);
    }

    private static ControlRegistry CreateControlRegistry() {
        var registry = new ControlRegistry();
        registry.Register("Label", (context, config) => new Kx.UI.Elements.Label(context, config.Id, config.Text ?? string.Empty, config.Font?.Size ?? 14f));
        registry.Register("Button", (context, config) => new Kx.UI.Elements.Button(context, config.Id, config.Text ?? string.Empty));
        registry.Register("ListBox", (context, config) => new Kx.UI.Elements.ListBox(context, config.Id));
        registry.Register("TextBox", (context, config) => new Kx.UI.Elements.TextBox(context, config.Id, config.Text ?? string.Empty));
        registry.Register("ProgressBar", (context, config) => new Kx.UI.Elements.ProgressBar(context, config.Id));
        registry.Register("StackPanel", (context, config) => new StackPanel(context, config.Id));
        return registry;
    }
}
