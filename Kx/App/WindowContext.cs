// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.Events;
using Kx.Abstractions.Rendering;
using Kx.Abstractions.WindowHost;
using Kx.Core.Configuration;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Core.Pipeline;
using Kx.UI.Manager;
using Kx.UI.Themes;
using Kx.Utility;

namespace Kx.App;

public sealed class WindowContext : IDisposable {
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
    public float DpiScale { get; set; } = 1f;

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
