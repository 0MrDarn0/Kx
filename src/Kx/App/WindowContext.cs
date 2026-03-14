// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.State;
using Kx.Sdk.WindowHost;
using Kx.Core.Configuration;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Core.Pipeline;
using Kx.UI.Manager;
using Kx.UI.Themes;
using Kx.Utility;

namespace Kx.App;

public sealed class WindowContext : IVisualContext, IDisposable {
    public IWindowSurface Target { get; }
    public IUiDispatcher UiThread { get; }
    public IWindowHost WindowHost { get; }
    public IResourceProvider Resources { get; }
    public IEventManager Events { get; }
    public AppConfig Config { get; }
    public UIElementManager UIElementManager { get; } = new();
    public FrameResource? Frame { get; private set; }
    public IWindowRenderer Renderer { get; private set; } = null!;
    public UpdaterPipelineRunner? Pipeline { get; private set; }

    private readonly object _frameLock = new();
    private IUiCommandRegistry? _commandRegistry;
    private IUiStateStore? _stateStore;
    private Action<string>? _openWindowAction;
    public float DpiScale { get; set; } = 1f;

    IUIElementManager IVisualContext.UIElementManager => UIElementManager;
    IUiCommandRegistry IVisualContext.Commands => _commandRegistry ?? throw new InvalidOperationException("No command registry has been registered for this context.");
    IUiStateStore IVisualContext.State => _stateStore ?? throw new InvalidOperationException("No state store has been registered for this context.");

    public WindowContext(
        IWindowSurface target,
        IUiDispatcher uiThread,
        IWindowHost windowHost,
        IEventManager? eventManager = null) {
        Target = target;
        UiThread = uiThread;
        WindowHost = windowHost;
        DpiScale = Math.Max(1f, Target.DeviceDpi / 96f);
        Config = ConfigLoader.Load<AppConfig>(Paths.GetConfig("app.yaml"));
        Resources = new FileResourceProvider(Paths.ResFolder);
        UIElementManager = new UIElementManager();
        Events = eventManager ?? new EventManager();
        LanguageLoader.Load(Config.Ui.Language);
        UIContextProvider.Initialize(this);
    }

    public void SetFrame(FrameResource frame) {
        ArgumentNullException.ThrowIfNull(frame);
        lock (_frameLock) {
            (Frame as IDisposable)?.Dispose();
            Frame = frame;
        }
    }

    public void SetRenderer(IWindowRenderer renderer) {
        Renderer = renderer;
    }

    /// <summary>
    /// Requests a new render pass for the current window.
    /// </summary>
    public void RequestRender() {
        Renderer.RequestRender();
    }

    /// <summary>
    /// Requests that the host window should close.
    /// </summary>
    public void CloseWindow() {
        WindowHost.CloseWindow();
    }

    /// <summary>
    /// Opens a named window definition inside the current host window.
    /// </summary>
    public void OpenWindow(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_openWindowAction is null)
            throw new InvalidOperationException("No window navigation handler has been registered for this context.");

        _openWindowAction(name);
    }

    internal void SetOpenWindowAction(Action<string> openWindowAction) {
        ArgumentNullException.ThrowIfNull(openWindowAction);
        _openWindowAction = openWindowAction;
    }

    internal void SetCommandRegistry(IUiCommandRegistry commandRegistry) {
        ArgumentNullException.ThrowIfNull(commandRegistry);
        _commandRegistry = commandRegistry;
    }

    internal void SetStateStore(IUiStateStore stateStore) {
        ArgumentNullException.ThrowIfNull(stateStore);
        _stateStore = stateStore;
    }

    public void SetPipeline(UpdaterPipelineRunner pipeline) {
        Pipeline = pipeline;
    }

    public void Dispose() {
        Renderer?.Dispose();
        UIElementManager.Dispose();
        (Frame as IDisposable)?.Dispose();
        Resources.Dispose();
        UIContextProvider.Clear();
    }
}
