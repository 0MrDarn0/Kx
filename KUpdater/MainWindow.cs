// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Core.Event;
using KUpdater.Core.Pipeline;
using KUpdater.Core.UI;
using KUpdater.Interop;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Theme;
using KUpdater.UI;
using KUpdater.Utility;

namespace KUpdater;

public partial class MainWindow : Window {
    public static MainWindow? Instance { get; private set; }

    private readonly MainTheme _theme;
    private readonly Renderer _renderer;
    private readonly IEventManager _eventManager;
    private readonly UpdaterPipelineRunner _runner;
    private readonly ControlManager _controlManager;
    private readonly BaseConfig _config;
    private readonly TrayIcon? _trayIcon;
    private readonly UIState _uiState = new();
    private readonly IResourceProvider _resourceProvider;

    public MainWindow() {
        Instance = this;

        LuaHost.OnNotify += (level, message) => {
            BeginInvoke(() => MessageBox.Show(this, message, level, MessageBoxButtons.OK, MessageBoxIcon.Information));
        };

        _config = new LuaConfig<BaseConfig>("base.lua", "Base").Load();
        _resourceProvider = new FileResourceProvider(Paths.ResFolder);
        _controlManager = new();
        _theme = new(this, _controlManager, _uiState, _config.Language, _resourceProvider);
        _eventManager = new EventManager(_theme);
        _renderer = new(this, _controlManager, _theme);
        _runner = new UpdaterPipelineRunner(_eventManager, new HttpUpdateSource(), _config.Url, AppDomain.CurrentDomain.BaseDirectory);

        _theme.ExposeToLua("Renderer", _renderer);
        _theme.ExposeToLua("EventManager", _eventManager);

        InitializeComponent();

        _trayIcon = new TrayIcon()
            .Name("kUpdater")
            .Icon(Paths.Resource("Default/app.ico"))
            .StatusIcons(status => status
                .Item("default", Paths.Resource("Default/app.ico"))
            )
            .Menu(menu => menu
                .Item("Settings", (s, e) => { })
                .Separator()
                .Exit((s, e) => Application.Exit()));
    }

    protected override CreateParams CreateParams {
        get {
            var cp = base.CreateParams;
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_LAYERED;
            return cp;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        _trayIcon?.Dispose();
        _renderer.Dispose();
        _theme.Dispose();
        _controlManager.Dispose();
        _resourceProvider?.Dispose();
        Instance = null;
        base.OnFormClosed(e);
    }

    protected override async void OnShown(EventArgs e) {
        base.OnShown(e);
        _renderer.RequestRender();

        // Events abonnieren
        _eventManager.Register<StatusEvent>(ev => {
            _uiState.SetStatus(ev.Text);
            _renderer.RequestRender();
        });

        _eventManager.Register<ProgressEvent>(ev => {
            _uiState.SetProgress(ev.Percent);
            _renderer.RequestRender();
        });

        _eventManager.Register<UpdateRequired>(_ => {
            _uiState.SetStartButtonVisible(false);
            _uiState.SetProgressVisible(true);
            _renderer.RequestRender();
        });

        _eventManager.Register<UpdatePipelineCompleted>(_ => {
            _uiState.SetProgressVisible(false);
            _uiState.SetStartButtonVisible(true);
            _renderer.RequestRender();
        });

        _eventManager.Register<ChangelogEvent>(ev => {
            _uiState.SetChangelog(ev.Text);
            _renderer.RequestRender();
        });

        // Pipeline starten
        await _runner.RunAsync(AppDomain.CurrentDomain.BaseDirectory);
    }

    protected override void RequestRender() => _renderer.RequestRender();

    // Weiterleitung der Input-Hooks an ControlManager
    protected override bool OnChildMouseMove(MouseEventArgs e) => _controlManager.MouseMove(e.Location);
    protected override bool OnChildMouseDown(MouseEventArgs e) => _controlManager.MouseDown(e.Location);
    protected override bool OnChildMouseUp(MouseEventArgs e) => _controlManager.MouseUp(e.Location);
    protected override bool OnChildMouseWheel(MouseEventArgs e) => _controlManager.MouseWheel(e.Delta, e.Location);

}
