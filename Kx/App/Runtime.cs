// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;
using Kx.Abstractions.Lifecycle;
using Kx.Abstractions.Logging;
using Kx.Abstractions.UI.Actions;
using Kx.Abstractions.UI.Commands;
using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.State;
using Kx.Abstractions.UI.Themes;
using Kx.Abstractions.WindowHost;
using Kx.Core.DI;
using Kx.Core.Lifecycle;
using Kx.Core.Logging;
using Kx.Core.Plugin;
using Kx.UI.Actions;
using Kx.UI.Commands;
using Kx.UI.Markup;
using Kx.UI.Platform;
using Kx.UI.State;
using Kx.UI.Themes;
using Kx.Utility;

namespace Kx.App;

public sealed class Runtime {

    private readonly MsDiContainer _services = new();
    private readonly PluginManager _pluginManager;
    private readonly IWindowHost _windowHost;
    private Action<IServiceRegistry>? _configureAppServices;
    private Task? _startTask;
    private bool _started;
    private Type? _windowType;
    private Window _window = null!;

    public Runtime(IWindowHost windowHost) {
        _windowHost = windowHost;
        _pluginManager = new PluginManager(_services);
    }

    public void Start() {
        StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Starts the runtime and runs startup lifecycle services before the main window is shown.
    /// </summary>
    public Task StartAsync() {
        if (_startTask is not null)
            return _startTask;

        if (_windowType is null)
            throw new InvalidOperationException("No window has been registered. Call RegisterWindow<TWindow>() before Start().");

        _startTask = StartCoreAsync();
        return _startTask;
    }

    private async Task StartCoreAsync() {
        _started = true;
        ConfigurePaths();
        ConfigureServices();
        _pluginManager.ConfigureServices();
        _services.Build();

        InitializePlugins();
        await StartupAsync();
        InitializeUI();
        _windowHost.ShowWindow();
    }

    public void RegisterWindow<TWindow>() where TWindow : Window {
        RegisterWindow(typeof(TWindow));
    }

    /// <summary>
    /// Registers application-specific services before the dependency container is built.
    /// </summary>
    /// <param name="configureServices">The callback used to register application services.</param>
    public void ConfigureServices(Action<IServiceRegistry> configureServices) {
        ArgumentNullException.ThrowIfNull(configureServices);

        if (_started)
            throw new InvalidOperationException("Application services must be configured before Start() is called.");

        _configureAppServices += configureServices;
    }

    internal void RegisterWindow(Type windowType) {
        ArgumentNullException.ThrowIfNull(windowType);

        if (!typeof(Window).IsAssignableFrom(windowType))
            throw new ArgumentException($"The window type must derive from {nameof(Window)}.", nameof(windowType));

        _windowType = windowType;
    }


    public async Task ShutdownAsync() {
        await _services.Get<ShutdownManager>().ShutdownAsync();
    }

    private async Task StartupAsync() {
        await _services.Get<StartupManager>().StartupAsync();
    }

    private static void ConfigurePaths() {
        Directory.CreateDirectory(Paths.LogFolder);
        Directory.CreateDirectory(Paths.PluginFolder);
        Directory.CreateDirectory(Paths.CfgFolder);
    }

    private void ConfigureServices() {
        var actionRegistry = new MarkupActionRegistry();
        var commandRegistry = new UiCommandRegistry();
        var stateStore = new UiStateStore();
        var controlRegistry = new ControlRegistry();
        var themeRegistry = new ThemeRegistry();
        var windowRegistry = new WindowRegistry();

        BuiltInMarkupActionRegistrar.Register(actionRegistry);
        BuiltInControlRegistrar.Register(controlRegistry);

        _services.Register<IWindowHost>(_windowHost);
        _services.Register<IMarkupActionRegistry>(actionRegistry);
        _services.Register<IUiCommandRegistry>(commandRegistry);
        _services.Register<IUiStateStore>(stateStore);
        _services.Register<IControlRegistry>(controlRegistry);
        _services.Register<IThemeRegistry>(themeRegistry);
        _services.Register<IWindowRegistry>(windowRegistry);

        // Startup
        _services.RegisterFactory<StartupManager>(Lifetime.Singleton, c => new StartupManager(c));

        // Shutdown
        _services.RegisterFactory<ShutdownManager>(Lifetime.Singleton, c => new ShutdownManager(c));
        _services.Register(_pluginManager);
        _services.Register<IShutdownAware>(_pluginManager);

        // Logging sinks
        _services.RegisterFactory<ILogSink>(Lifetime.Singleton,
            c => new AsyncLogSink(new DebugSink()));

        _services.RegisterFactory<ILogSink>(Lifetime.Singleton,
            c => new AsyncLogSink(
                new DailyRollingFileSink(
                    5 * 1024 * 1024,
                    5,
                    () => Paths.GetDailyLogFile()
                )
            )
        );

        // LoggerFactory
        _services.RegisterFactory<ILoggerFactory>(Lifetime.Singleton, c => new LoggerFactory(c));

        // System logger
        _services.RegisterFactory<ILoggingService>(Lifetime.Singleton,
            c => c.Get<ILoggerFactory>().CreateLogger("System"));

        // TrayIcon services
        _services.Register(new TrayIcon());
        _services.Register<ITrayService, TrayIconService>(Lifetime.Singleton);

        _configureAppServices?.Invoke(_services);
    }

    private void InitializePlugins() {
        _pluginManager.InitializeAll();
    }

    private void InitializeUI() {
        _window = (Window)_services.Create(_windowType!);

        _windowHost.Closed += async e => {
            await ShutdownAsync();
        };
    }
}
