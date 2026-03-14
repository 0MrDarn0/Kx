using Kx.Abstractions.UI.Markup;
using Kx.UI.Actions;
using Kx.UI.Elements.Panel;
using Kx.UI.Markup;
using Kx.Tests.TestInfrastructure;

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

    private static ControlRegistry CreateControlRegistry() {
        var registry = new ControlRegistry();
        registry.Register("Label", (context, config) => new Kx.UI.Elements.Label(context, config.Id, config.Text ?? string.Empty, config.Font?.Size ?? 14f));
        registry.Register("Button", (context, config) => new Kx.UI.Elements.Button(context, config.Id, config.Text ?? string.Empty));
        registry.Register("StackPanel", (context, config) => new StackPanel(context, config.Id));
        return registry;
    }
}
