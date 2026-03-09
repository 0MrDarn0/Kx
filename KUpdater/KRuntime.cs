// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Backend;
using KUpdater.Abstractions.DI;
using KUpdater.Abstractions.Logging;
using KUpdater.Abstractions.Plugin;
using KUpdater.Core.DI;
using KUpdater.Core.Lifecycle;
using KUpdater.Core.Logging;
using KUpdater.Core.Plugin;
using KUpdater.UI.Platform;
using KUpdater.Utility;

namespace KUpdater;

public sealed class KRuntime(IWindowBackend backend) {

    private readonly IDependencyContainer _container = new MsDiContainer();
    private Window _window = null!;

    public void Start() {
        ConfigurePaths();
        ConfigureServices();
        _container.Build();

        InitializePlugins();
        InitializeUI();
    }

    public async Task ShutdownAsync() {
        await _container.Get<ShutdownManager>().ShutdownAsync();
    }

    private static void ConfigurePaths() {
        Directory.CreateDirectory(Paths.LogFolder);
        Directory.CreateDirectory(Paths.PluginFolder);
        Directory.CreateDirectory(Paths.CfgFolder);
    }

    private void ConfigureServices() {
        // Shutdown
        _container.RegisterFactory<ShutdownManager>(Lifetime.Singleton, c => new ShutdownManager(c));

        // Logging sinks
        _container.RegisterFactory<ILogSink>(Lifetime.Singleton,
            c => new AsyncLogSink(new DebugSink()));

        _container.RegisterFactory<ILogSink>(Lifetime.Singleton,
            c => new AsyncLogSink(
                new DailyRollingFileSink(
                    5 * 1024 * 1024,
                    5,
                    () => Paths.GetDailyLogFile()
                )
            )
        );

        // LoggerFactory
        _container.RegisterFactory<ILoggerFactory>(Lifetime.Singleton, c => new LoggerFactory(c));

        // System logger
        _container.RegisterFactory<ILoggingService>(Lifetime.Singleton,
            c => c.Get<ILoggerFactory>().CreateLogger("System"));

        // TrayIcon services
        _container.Register(new TrayIcon());
        _container.Register<ITrayService, TrayIconService>(Lifetime.Singleton);

        // Window
        _container.RegisterFactory<Window>(Lifetime.Transient,
            c => new Window(backend, c.Get<ITrayService>(), c.Get<ILoggingService>()));
    }

    private void InitializePlugins() {
        var log = _container.Get<ILoggingService>();
        var plugins = PluginLoader.LoadAll<IPlugin>();

        foreach (var plugin in plugins) {
            log.Info($"Loading plugin: {plugin.Name}");
            plugin.Initialize(new PluginContext(_container, plugin.Name));
        }
    }

    private void InitializeUI() {
        _window = _container.Get<Window>();

        backend.Shown += e => _window.OnShown();
        backend.Closed += async e => {
            _window.OnClosed(e.UserInitiated);
            await ShutdownAsync();
        };
        backend.StateChanged += e => _window.OnStateChanged(e.State);
        backend.FocusChanged += e => _window.OnFocusChanged(e.State);
    }
}
