// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Skin;
using KUpdater.UI;
using KUpdater.UI.Interface;
using KUpdater.Utility;

namespace KUpdater.Core;

public sealed class WindowContext : IDisposable {
    public IRenderTarget Target { get; }
    public IUiThreadInvoker UiThread { get; }
    public BaseConfig Config { get; }
    public IResourceProvider Resources { get; }
    public ControlManager Controls { get; }
    public IEventManager Events { get; }

    public ISkin Skin { get; private set; } = null!;
    public IRenderer Renderer { get; private set; } = null!;
    public UpdaterPipelineRunner? Pipeline { get; private set; }

    public WindowContext(
        IRenderTarget target,
        IUiThreadInvoker uiThread,
        IEventManager? eventManager = null) {
        Target = target;
        UiThread = uiThread;

        Config = new LuaConfig<BaseConfig>("base.lua", "Base").Load();
        Resources = new FileResourceProvider(Paths.ResFolder);
        Controls = new ControlManager();
        Events = eventManager ?? new EventManager();

        UIContextProvider.Initialize(this);
    }

    public void SetSkin(ISkin skin) {
        Skin = skin;
        Events.SetSkin(skin);
    }

    public void SetRenderer(IRenderer renderer) {
        Renderer = renderer;
    }

    public void SetPipeline(UpdaterPipelineRunner pipeline) {
        Pipeline = pipeline;
    }

    public void Dispose() {
        Renderer?.Dispose();
        Skin?.Dispose();
        Controls.Dispose();
        Resources.Dispose();
        UIContextProvider.Clear();
    }
}
