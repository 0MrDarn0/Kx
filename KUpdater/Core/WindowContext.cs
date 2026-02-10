// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Core.UI;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Skin;
using KUpdater.UI;
using KUpdater.UI.Interface;
using KUpdater.Utility;

namespace KUpdater.Core;

public sealed class WindowContext : IDisposable {
    public IRenderTarget Target { get; }
    public IUiThreadInvoker UiThread { get; }
    public SkinBase Skin { get; }
    public ControlManager Controls { get; }
    public IEventManager Events { get; }
    public UIState State { get; }
    public BaseConfig Config { get; }
    public IResourceProvider Resources { get; }
    public IRenderer Renderer { get; }
    public UpdaterPipelineRunner Pipeline { get; }

    public WindowContext(
        IRenderTarget target,
        IUiThreadInvoker uiThread,
        Func<WindowContext, SkinBase> skinFactory,
        Func<WindowContext, IRenderer>? rendererFactory = null) {

        Target = target;
        UiThread = uiThread;
        Config = new LuaConfig<BaseConfig>("base.lua", "Base").Load();
        Resources = new FileResourceProvider(Paths.ResFolder);
        Controls = new ControlManager();
        Events = new EventManager();
        State = new UIState();

        UIContextProvider.Initialize(this);

        Skin = skinFactory(this);
        Renderer = (rendererFactory ?? (ctx => new Renderer(ctx)))(this);
        Pipeline = new UpdaterPipelineRunner(Events, new HttpUpdateSource(), Config.Url, AppDomain.CurrentDomain.BaseDirectory);
    }

    public void Dispose() {
        Renderer.Dispose();
        Skin.Dispose();
        Controls.Dispose();
        Resources.Dispose();
        UIContextProvider.Clear();
    }
}
