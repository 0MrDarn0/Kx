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

public partial class MainForm : Form {
    public static MainForm? Instance { get; private set; }

    private bool _isDragging = false;
    private Point _dragStart;

    private bool _isResizing = false;
    private Point _resizeStartCursor;
    private Size _resizeStartSize;
    private readonly int _resizeHitSize = 40;

    private readonly MainTheme _theme;
    private readonly Renderer _renderer;
    private readonly IEventManager _eventManager;
    private readonly UpdaterPipelineRunner _runner;
    private readonly ControlManager _controlManager;
    private readonly BaseConfig _config;
    private readonly TrayIcon? _trayIcon;
    private readonly UIState _uiState = new();
    private readonly IResourceProvider _resourceProvider;

    public MainForm() {
        Instance = this;

        LuaHost.OnNotify += (level, message) => {
            BeginInvoke(() => MessageBox.Show(this, message, level, MessageBoxButtons.OK, MessageBoxIcon.Information));
        };

        _config = new LuaConfig<BaseConfig>("base.lua", "Base").Load();

        _eventManager = new EventManager();
        _runner = new UpdaterPipelineRunner(_eventManager, new HttpUpdateSource(), _config.Url, AppDomain.CurrentDomain.BaseDirectory);

        _resourceProvider = new FileResourceProvider(Paths.ResFolder);
        _controlManager = new();
        _theme = new(this, _controlManager, _uiState, _config.Language, _resourceProvider);
        _renderer = new(this, _controlManager, _theme);

        InitializeComponent();
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

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


    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        _renderer.RequestRender();
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        if (_isResizing) {
            Point delta = new(
           Cursor.Position.X - _resizeStartCursor.X,
           Cursor.Position.Y - _resizeStartCursor.Y);

            // Bildschirm-Arbeitsbereich holen (ohne Taskleiste)
            Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;

            // Dynamische Maximalwerte
            int maxWidth = workArea.Width;
            int maxHeight = workArea.Height;

            // Neue Größe berechnen
            int newWidth = _resizeStartSize.Width + delta.X;
            int newHeight = _resizeStartSize.Height + delta.Y;

            // Mindest- und Höchstwerte anwenden
            newWidth = Math.Max(450, Math.Min(newWidth, maxWidth));
            newHeight = Math.Max(300, Math.Min(newHeight, maxHeight));

            // Nur Größe setzen – Rest macht OnResize
            this.Size = new Size(newWidth, newHeight);
            return;
        }

        if (_isDragging) {
            Point newLocation = new(this.Left + e.X - _dragStart.X, this.Top + e.Y - _dragStart.Y);
            this.Location = newLocation;
            return;
        }

        this.Cursor = new Rectangle(
            this.Width - _resizeHitSize,
            this.Height - _resizeHitSize,
            _resizeHitSize,
            _resizeHitSize
        ).Contains(e.Location) ? Cursors.SizeNWSE : Cursors.Default;

        // Let ControlManager handle hover state for all controls
        if (_controlManager.MouseMove(e.Location))
            _renderer.RequestRender();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        if (e.Button != MouseButtons.Left)
            return;

        // Erst an ControlManager weitergeben
        bool handled = _controlManager.MouseDown(e.Location);
        if (handled) {
            _renderer.RequestRender();
            return; // Wenn ein Element reagiert, nicht weiterziehen!
        }

        // Resize hotspot
        Rectangle resizeRect = new(this.Width - _resizeHitSize, this.Height - _resizeHitSize, _resizeHitSize, _resizeHitSize);
        if (resizeRect.Contains(e.Location)) {
            _isResizing = true;
            _resizeStartCursor = Cursor.Position;
            _resizeStartSize = this.Size;
            return;
        }

        // Fensterbewegung starten
        _isDragging = true;
        _dragStart = e.Location;
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        _isDragging = false;
        _isResizing = false;

        // Pass to ControlManager so controls can handle clicks
        if (_controlManager.MouseUp(e.Location))
            _renderer.RequestRender();
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
        bool handled = _controlManager.MouseWheel(e.Delta, e.Location);
        if (handled)
            _renderer.RequestRender();
    }

}
