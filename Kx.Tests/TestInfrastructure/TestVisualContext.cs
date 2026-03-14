using Kx.Abstractions.Events;
using Kx.Abstractions.UI;
using Kx.Abstractions.UI.Commands;
using Kx.Abstractions.UI.State;
using Kx.Abstractions.WindowHost;
using Kx.Core.Event;
using Kx.UI.Commands;
using Kx.UI.Manager;
using Kx.UI.State;

namespace Kx.Tests.TestInfrastructure;

internal sealed class TestVisualContext : IVisualContext {
    public float DpiScale { get; set; } = 1f;
    public IUiDispatcher UiThread { get; } = new TestUiDispatcher();
    public IUIElementManager UIElementManager { get; } = new UIElementManager();
    public IEventManager Events { get; } = new EventManager();
    public IUiCommandRegistry Commands { get; } = new UiCommandRegistry();
    public IUiStateStore State { get; } = new UiStateStore();
    public int RenderRequestCount { get; private set; }
    public string? OpenedWindowName { get; private set; }
    public bool CloseRequested { get; private set; }

    public void RequestRender() {
        RenderRequestCount++;
    }

    public void CloseWindow() {
        CloseRequested = true;
    }

    public void OpenWindow(string name) {
        OpenedWindowName = name;
    }
}

internal sealed class TestUiDispatcher : IUiDispatcher {
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
