using System.Drawing;

using Kx.Core.Event;
using Kx.Sdk.Events;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Elements;
using Kx.Sdk.UI.State;
using Kx.Sdk.UI.VisualTree;
using Kx.Sdk.WindowHost;
using Kx.UI.Commands;
using Kx.UI.Manager;
using Kx.UI.State;

namespace Kx.Tests;

public sealed class UIElementManagerInteractionTests {
    [Fact]
    public void WhenChildDoesNotHandleMouseDownThenParentStillReceivesIt() {
        var context = new TestVisualContext();
        using var parent = new TestContainer(context, "parent") {
            FixedBounds = new Rectangle(0, 0, 40, 40)
        };
        using var child = new PassiveChild(context, "child") {
            FixedBounds = new Rectangle(0, 0, 40, 40)
        };

        parent.AddChild(child);
        parent.Arrange(new Rectangle(0, 0, 40, 40), 1f);
        child.Arrange(new Rectangle(0, 0, 40, 40), 1f);
        context.UIElementManager.Add(parent);

        bool handled = context.UIElementManager.MouseDown(new Point(10, 10));

        Assert.True(handled);
        Assert.Equal(1, parent.MouseDownCount);
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

    private sealed class TestContainer(IVisualContext context, string id) : UIElement(context, id), IVisualContainer {
        private readonly List<IVisual> _children = [];

        public IReadOnlyList<IVisual> Children => _children;
        public int MouseDownCount { get; private set; }

        public void AddChild(UIElement child) {
            ArgumentNullException.ThrowIfNull(child);
            child.SetParent(this);
            _children.Add(child);
        }

        public override bool OnMouseDown(Point p) {
            MouseDownCount++;
            return true;
        }

        public override void Measure(float dpi) {
            base.Measure(dpi);
        }

        protected override void OnDraw(IKxCanvas canvas) {
        }
    }

    private sealed class PassiveChild(IVisualContext context, string id) : UIElement(context, id) {
        protected override void OnDraw(IKxCanvas canvas) {
        }
    }
}
