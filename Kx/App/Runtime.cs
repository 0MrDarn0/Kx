// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;
using Kx.Abstractions.Lifecycle;
using Kx.Abstractions.Logging;
using Kx.Abstractions.WindowHost;
using Kx.Core.DI;
using Kx.Core.Lifecycle;
using Kx.Core.Logging;
using Kx.Core.Plugin;
using Kx.UI.Platform;
using Kx.Utility;

namespace Kx.App;

public sealed class Runtime(IWindowHost windowHost) {

    private readonly MsDiContainer _services = new();
    private bool _started;
    private Type? _windowType;
    private Window _window = null!;

    public void Start() {
        if (_started)
            return;

        if (_windowType is null)
            throw new InvalidOperationException("No window has been registered. Call RegisterWindow<TWindow>() before Start().");

        _started = true;
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
            c => c.Create<TWindow>());
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
        _services.Register<IWindowHost>(windowHost);

        // Shutdown
        _services.RegisterFactory<ShutdownManager>(Lifetime.Singleton, c => new ShutdownManager(c));
        _services.RegisterFactory<PluginManager>(Lifetime.Singleton, c => new PluginManager(c));
        _services.RegisterFactory<IShutdownAware>(Lifetime.Singleton, c => c.Get<PluginManager>());

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
        _services.Get<PluginManager>().InitializeAll();
    }

    private void InitializeUI() {
        _window = (Window)_services.Get(_windowType!);

        windowHost.Closed += async e => {
            await ShutdownAsync();
        };
    }
}
