// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.DI;
using Kx.Core.Plugin;
using Kx.Sdk.DI;
using Kx.Sdk.WindowHost;
using Kx.Utility;

namespace Kx.App;

public sealed class Runtime {

    private readonly MsDiContainer _services = new();
    private readonly PluginRuntimeComposition _pluginComposition;
    private readonly RuntimeUiComposition _uiComposition;
    private readonly RuntimeLoggingComposition _loggingComposition;
    private readonly RuntimeShellComposition _shellComposition;
    private readonly PluginManager _pluginManager;
    private readonly RuntimeLifecycleCoordinator _lifecycleCoordinator;
    private readonly RuntimeWindowCoordinator _windowCoordinator;
    private readonly IWindowHost _windowHost;
    private Action<IServiceRegistry>? _configureAppServices;
    private Task? _startTask;
    private bool _started;
    private Type? _windowType;
    private Window _window = null!;

    public Runtime(IWindowHost windowHost) {
        _windowHost = windowHost;
        _pluginComposition = new PluginRuntimeComposition();
        _uiComposition = new RuntimeUiComposition();
        _loggingComposition = new RuntimeLoggingComposition();
        _shellComposition = new RuntimeShellComposition();
        _pluginManager = new PluginManager(_services, _pluginComposition.Policy);
        _lifecycleCoordinator = new RuntimeLifecycleCoordinator(_services, _pluginManager);
        _windowCoordinator = new RuntimeWindowCoordinator(_services, _windowHost);
    }

    public void Start() {
        GlobalExceptionHandler.RegisterShutdownHandler(ShutdownAsync);
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

        GlobalExceptionHandler.RegisterShutdownHandler(async () => {
            try {
                await ShutdownAsync().ConfigureAwait(false);
            }
            catch {
                Environment.Exit(1);
            }
        });

        await _lifecycleCoordinator.StartAsync();
        _window = _windowCoordinator.Show(_windowType!, ShutdownAsync);
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
        await _lifecycleCoordinator.ShutdownAsync();
    }

    private static void ConfigurePaths() {
        Directory.CreateDirectory(Paths.LogFolder);
        Directory.CreateDirectory(Paths.PluginFolder);
        Directory.CreateDirectory(Paths.CfgFolder);
    }

    private void ConfigureServices() {
        RuntimeServiceConfiguration.RegisterDefaults(_services, _windowHost, _pluginManager, _uiComposition, _loggingComposition, _shellComposition);
        _configureAppServices?.Invoke(_services);
    }
}
