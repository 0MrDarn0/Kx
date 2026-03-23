using System.Drawing;

using Kx.Core.Event;
using Kx.Sdk.Events;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.State;
using Kx.Sdk.WindowHost;
using Kx.UI.Commands;
using Kx.UI.Manager;
using Kx.UI.State;

namespace Kx.Tests;

public sealed class ListBoxInteractionTests {
    [Fact]
    public void WhenListBoxItemIsClickedThenSelectionChanges() {
        var context = new TestVisualContext();
        using var listBox = new Kx.UI.Elements.ListBox(context, "news") {
            FixedBounds = new Rectangle(0, 0, 120, 90)
        };
        listBox.SetItems(["First", "Second", "Third"]);
        listBox.Arrange(new Rectangle(0, 0, 120, 90), 1f);

        listBox.OnMouseDown(new Point(10, 35));

        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void WhenSelectedIndexBindingChangesThenListBoxSelectionUpdates() {
        var context = new TestVisualContext();
        var registry = new Kx.UI.Markup.ControlRegistry();
        registry.Register("ListBox", (visualContext, config) => new Kx.UI.Elements.ListBox(visualContext, config.Id));
        var actions = new Kx.UI.Actions.MarkupActionRegistry();
        context.State.Set("updater.news.items", new[] { "First", "Second", "Third" });
        context.State.Set("updater.news.selectedIndex", 0);

        var control = Kx.UI.Markup.ControlFactory.Create(registry, actions, context, new Kx.Sdk.UI.Markup.ControlConfig {
            Type = "ListBox",
            Id = "news",
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["itemsBinding"] = "updater.news.items",
                ["selectedIndexBinding"] = "updater.news.selectedIndex"
            }
        });

        var listBox = Assert.IsType<Kx.UI.Elements.ListBox>(control);
        context.State.Set("updater.news.selectedIndex", 2);

        Assert.Equal(2, listBox.SelectedIndex);
    }

    private sealed class TestVisualContext : IVisualContext {
        public float DpiScale => 1f;
        public IUiDispatcher UiThread { get; } = new ImmediateDispatcher();
        public UIElementManager UIElementManager { get; } = new();
        IUIElementManager IVisualContext.UIElementManager => UIElementManager;
        public IEventManager Events { get; } = new EventManager();
        public IUiCommandRegistry Commands { get; } = new UiCommandRegistry();
        public IUiStateStore State { get; } = new UiStateStore();

        public void RequestRender() {
        }

        public void CloseWindow() {
        }

        public void OpenWindow(string name) {
        }
    }

    private sealed class ImmediateDispatcher : IUiDispatcher {
        public bool InvokeRequired => false;

        public void BeginInvoke(Delegate d) {
            ArgumentNullException.ThrowIfNull(d);
            d.DynamicInvoke();
        }

        public void Invoke(Action action) {
            ArgumentNullException.ThrowIfNull(action);
            action();
        }
    }
}
