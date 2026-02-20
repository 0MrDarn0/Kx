// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Backend.BackendAbstractions;
using KUpdater.Core.Configuration;
using KUpdater.Core.Event;
using KUpdater.Core.Localization;
using KUpdater.Core.Pipeline;
using KUpdater.UI.Manager;
using KUpdater.UI.Rendering;
using KUpdater.UI.Themes;
using KUpdater.Utility;

namespace KUpdater.Core;

public sealed class WindowContext : IDisposable {
    public IRenderTarget Target { get; }
    public IUiThreadInvoker UiThread { get; }
    public IWindowBackend Backend { get; }
    public IResourceProvider Resources { get; }
    public IEventManager Events { get; }
    public AppConfig Config { get; }
    public UIElementManager UIElementManager { get; } = new();
    public FrameResource? Frame { get; private set; }
    public IWindowRenderer Renderer { get; private set; } = null!;
    public UpdaterPipelineRunner? Pipeline { get; private set; }

    private readonly object _frameLock = new();
    public float DpiScale { get; set; } = 1f;

    public WindowContext(
        IRenderTarget target,
        IUiThreadInvoker uiThread,
        IWindowBackend backend,
        IEventManager? eventManager = null) {
        Target = target;
        UiThread = uiThread;
        Backend = backend;
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
