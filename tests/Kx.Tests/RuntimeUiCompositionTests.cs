using Kx.App;
using Kx.Core.DI;
using Kx.Core.Plugin;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.WindowHost;
using Kx.Tests.TestInfrastructure;

namespace Kx.Tests;

public sealed class RuntimeUiCompositionTests {
    [Fact]
    public void WhenCompositionIsCreatedThenBuiltInCloseWindowActionIsAvailable() {
        var composition = new RuntimeUiComposition();
        var context = new TestVisualContext();
        var source = new TestElement(context, "root");

        var executed = composition.ActionRegistry.TryExecute(new TestMarkupActionContext(context, source, "closeWindow"));

        Assert.True(executed);
        Assert.True(context.CloseRequested);
    }

    [Fact]
    public void WhenCompositionIsCreatedThenBuiltInLabelControlCanBeCreated() {
        var composition = new RuntimeUiComposition();
        var context = new TestVisualContext();
        var config = new ControlConfig {
            Type = "Label",
            Id = "title",
            Text = "Hello"
        };

        var created = composition.ControlRegistry.TryCreate(context, config, out var control);

        Assert.True(created);
        Assert.IsType<Kx.UI.Elements.Label>(control);
    }

    [Fact]
    public void WhenDefaultsAreRegisteredWithCompositionThenContainerUsesTheSameUiInstances() {
        var services = new MsDiContainer();
        var windowHost = new TestWindowHost();
        var pluginManager = new PluginManager(services);
        var composition = new RuntimeUiComposition();

        RuntimeServiceConfiguration.RegisterDefaults(services, windowHost, pluginManager, composition);
        services.Build();

        Assert.Same(composition.ActionRegistry, services.Get<IMarkupActionRegistry>());
        Assert.Same(composition.ControlRegistry, services.Get<IControlRegistry>());
        Assert.Same(composition.WindowContentRegistry, services.Get<IWindowContentRegistry>());
        Assert.Same(windowHost, services.Get<IWindowHost>());
    }

    private sealed class TestMarkupActionContext(IVisualContext visualContext, Kx.Sdk.UI.Elements.UIElement source, string actionName, string? argument = null) : IMarkupActionContext {
        public IVisualContext VisualContext { get; } = visualContext;
        public Kx.Sdk.UI.Elements.UIElement Source { get; } = source;
        public string ActionName { get; } = actionName;
        public string? Argument { get; } = argument;
    }
}
