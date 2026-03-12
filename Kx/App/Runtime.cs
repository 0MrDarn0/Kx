// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;
using Kx.Abstractions.Logging;
using Kx.Abstractions.Plugin;
using Kx.Abstractions.WindowHost;
using Kx.Core.DI;
using Kx.Core.Lifecycle;
using Kx.Core.Logging;
using Kx.Core.Plugin;
using Kx.UI.Platform;
using Kx.Utility;

using Microsoft.Extensions.DependencyInjection;

namespace Kx.App;

public sealed class Runtime(IWindowHost windowHost) {

    private readonly MsDiContainer _services = new();
    private Type? _windowType;
    private Window _window = null!;

    public void Start() {
        ConfigurePaths();
        ConfigureServices();
        _services.Build();

        InitializePlugins();
        InitializeUI();
        windowHost.ShowWindow();
    }

    public void RegisterWindow<TWindow>() where TWindow : Window {
        _windowType = typeof(TWindow);

        _services.RegisterFactory<TWindow>(Lifetime.Transient,
            c => ActivatorUtilities.CreateInstance<TWindow>(
                _services.Provider,
                windowHost,
                c.Get<ITrayService>(),
                c.Get<ILoggingService>()
            ));
    }


    public async Task ShutdownAsync() {
        await _services.Get<ShutdownManager>().ShutdownAsync();
    }

    private static void ConfigurePaths() {
        Directory.CreateDirectory(Paths.LogFolder);
        Directory.CreateDirectory(Paths.PluginFolder);
        Directory.CreateDirectory(Paths.CfgFolder);
    }

    private void ConfigureServices() {
        // Shutdown
        _services.RegisterFactory<ShutdownManager>(Lifetime.Singleton, c => new ShutdownManager(c));

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
    }

    private void InitializePlugins() {
        var log = _services.Get<ILoggingService>();
        var plugins = PluginLoader.LoadAll<IPlugin>();

        foreach (var plugin in plugins) {
            log.Info($"Loading plugin: {plugin.Name}");
            plugin.Initialize(new PluginContext(_services, plugin.Name));
        }
    }

    private void InitializeUI() {
        _window = (Window)_services.Get(_windowType!);

        windowHost.Closed += async e => {
            _window.RaiseClosed(e.UserInitiated);
            await ShutdownAsync();
        };
    }
}
