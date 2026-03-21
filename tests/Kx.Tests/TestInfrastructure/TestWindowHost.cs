using Kx.Sdk.Events;
using Kx.Sdk.WindowHost;

namespace Kx.Tests.TestInfrastructure;

internal sealed class TestWindowHost : IWindowHost {
    public event Action<ShownEvent>? Shown;
    public event Action<ClosedEvent>? Closed;
    public event Action<ResizeEvent>? Resized;
    public event Action<MouseEvent>? MouseMove;
    public event Action<MouseEvent>? MouseDown;
    public event Action<MouseEvent>? MouseUp;
    public event Action<MouseEvent>? MouseWheel;
    public event Action<KeyEvent>? KeyDown;
    public event Action<KeyEvent>? KeyUp;
    public event Action<TextInputEvent>? TextInput;
    public event Action<StateEvent>? StateChanged;
    public event Action<FocusEvent>? FocusChanged;

    public IntPtr Handle => IntPtr.Zero;
    public bool IsDisposed => false;
    public bool IsHandleCreated => true;
    public int Left => 0;
    public int Top => 0;
    public int Width => 800;
    public int Height => 600;
    public int DeviceDpi => 96;
    public bool InvokeRequired => false;
    public object? Cursor { get; set; }
    public int ShowWindowCallCount { get; private set; }
    public int CloseWindowCallCount { get; private set; }

    public void BeginInvoke(Delegate d) {
        ArgumentNullException.ThrowIfNull(d);
        d.DynamicInvoke();
    }

    public void Invoke(Action action) {
        ArgumentNullException.ThrowIfNull(action);
        action();
    }

    public void SetSize(int width, int height) {
    }

    public void SetPosition(int x, int y) {
    }

    public void ShowWindow() {
        ShowWindowCallCount++;
        Shown?.Invoke(new ShownEvent());
    }

    public void CloseWindow() {
        CloseWindowCallCount++;
    }

    public void RaiseClosed(bool userInitiated = true) {
        Closed?.Invoke(new ClosedEvent(userInitiated));
    }
}
