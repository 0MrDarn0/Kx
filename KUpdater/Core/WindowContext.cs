// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Plugin;
using KUpdater.Backend.BackendAbstractions;
using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Scripting.Runtime;
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
    public BaseConfig Config { get; }
    public IResourceProvider Resources { get; }
    public ControlManager Controls { get; }
    public IEventManager Events { get; }
    public FrameResources Frame { get; private set; } = null!;
    public IRenderer Renderer { get; private set; } = null!;
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
        Config = new LuaConfig<BaseConfig>("base.lua", "Base").Load();
        Resources = new FileResourceProvider(Paths.ResFolder);
        Controls = new ControlManager();
        Events = eventManager ?? new EventManager();
        ContentRoot = new ContentRoot(this);
        Controls.Add(ContentRoot);

        UIContextProvider.Initialize(this);
    }

    public void SetFrame(FrameResources frame) {
        Frame = frame;
    }

    public void SetRenderer(IRenderer renderer) {
        Renderer = renderer;
    }

    public void SetPipeline(UpdaterPipelineRunner pipeline) {
        Pipeline = pipeline;
    }

    public void Dispose() {
        Renderer?.Dispose();
        Controls.Dispose();
        Resources.Dispose();
        UIContextProvider.Clear();
    }
}
