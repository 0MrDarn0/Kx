// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Core.UI;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Skin;
using KUpdater.UI;
using KUpdater.Utility;

namespace KUpdater.Core;

public sealed class WindowContext : IDisposable {
    public MainWindow Window { get; }
    public ControlManager Controls { get; }
    public IEventManager Events { get; }
    public UIState State { get; }
    public BaseConfig Config { get; }
    public IResourceProvider Resources { get; }
    public MainWindowSkin Skin { get; }
    public Renderer Renderer { get; }
    public UpdaterPipelineRunner Pipeline { get; }

    public WindowContext(MainWindow window) {
        Window = window;

        Config = new LuaConfig<BaseConfig>("base.lua", "Base").Load();
        Resources = new FileResourceProvider(Paths.ResFolder);
        Controls = new ControlManager();
        Events = new EventManager();
        State = new UIState();
        Skin = new MainWindowSkin(this);

        Renderer = new Renderer(window, Controls, Skin, Config);
        Pipeline = new UpdaterPipelineRunner(Events, new HttpUpdateSource(), Config.Url, AppDomain.CurrentDomain.BaseDirectory);
    }

    public void Dispose() {
        Renderer.Dispose();
        Skin.Dispose();
        Controls.Dispose();
        Resources.Dispose();
    }
}
