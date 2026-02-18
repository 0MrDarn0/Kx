// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Plugin;
using KUpdater.Backend.BackendAbstractions;
using KUpdater.Core.Configuration;
using KUpdater.Core.Event;
using KUpdater.Core.Localization;
using KUpdater.Core.Pipeline;
using KUpdater.UI;
using KUpdater.UI.Control;
using KUpdater.UI.Manager;
using KUpdater.UI.Rendering;
using KUpdater.UI.Themes;
using KUpdater.Utility;

namespace KUpdater.Core;

public sealed class WindowContext : IDisposable, IPluginContext {
    public IRenderTarget Target { get; }
    public IUiThreadInvoker UiThread { get; }
    public IWindowBackend Backend { get; }
    public AppConfig Config { get; }
    public IResourceProvider Resources { get; }
    public ControlManager Controls { get; }
    public IEventManager Events { get; }

    private readonly object _frameLock = new();
    public FrameResource? Frame { get; private set; }
    public IWindowRenderer Renderer { get; private set; } = null!;
    public UpdaterPipelineRunner? Pipeline { get; private set; }
    public ContentRoot ContentRoot { get; private set; }
    object IPluginContext.Services => this;

    public WindowContext(
        IRenderTarget target,
        IUiThreadInvoker uiThread,
        IWindowBackend backend,
        IEventManager? eventManager = null) {
        Target = target;
        UiThread = uiThread;
        Backend = backend;
        Config = ConfigLoader.Load<AppConfig>(Paths.GetConfig("app.yaml"));
        Resources = new FileResourceProvider(Paths.ResFolder);
        Controls = new ControlManager();
        Events = eventManager ?? new EventManager();
        ContentRoot = new ContentRoot(this);
        Controls.Add(ContentRoot);

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
        Controls.Dispose();
        (Frame as IDisposable)?.Dispose();
        Resources.Dispose();
        UIContextProvider.Clear();
    }
}
