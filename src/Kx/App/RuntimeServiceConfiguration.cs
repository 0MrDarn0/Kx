// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Lifecycle;
using Kx.Sdk.Logging;
using Kx.Sdk.WindowHost;
using Kx.Core.Plugin;
using Kx.UI.Platform;

namespace Kx.App;

/// <summary>
/// Registers the built-in services required by the Kx runtime before the dependency container is built.
/// </summary>
public static class RuntimeServiceConfiguration {
    /// <summary>
    /// Registers the default runtime services for a window host and plugin manager.
    /// </summary>
    /// <param name="services">The service registry to populate.</param>
    /// <param name="windowHost">The window host used by the runtime.</param>
    /// <param name="pluginManager">The plugin manager participating in startup and shutdown.</param>
    public static void RegisterDefaults(IServiceRegistry services, IWindowHost windowHost, PluginManager pluginManager) {
        RegisterDefaults(services, windowHost, pluginManager, new RuntimeUiComposition(), new RuntimeLoggingComposition(), new RuntimeShellComposition());
    }

    /// <summary>
    /// Registers the default runtime services for a window host, plugin manager, and shared UI composition.
    /// </summary>
    /// <param name="services">The service registry to populate.</param>
    /// <param name="windowHost">The window host used by the runtime.</param>
    /// <param name="pluginManager">The plugin manager participating in startup and shutdown.</param>
    /// <param name="uiComposition">The shared UI composition used by the runtime.</param>
    public static void RegisterDefaults(IServiceRegistry services, IWindowHost windowHost, PluginManager pluginManager, RuntimeUiComposition uiComposition) {
        RegisterDefaults(services, windowHost, pluginManager, uiComposition, new RuntimeLoggingComposition(), new RuntimeShellComposition());
    }

    /// <summary>
    /// Registers the default runtime services for a window host, plugin manager, shared UI composition, and shared logging composition.
    /// </summary>
    /// <param name="services">The service registry to populate.</param>
    /// <param name="windowHost">The window host used by the runtime.</param>
    /// <param name="pluginManager">The plugin manager participating in startup and shutdown.</param>
    /// <param name="uiComposition">The shared UI composition used by the runtime.</param>
    /// <param name="loggingComposition">The shared logging composition used by the runtime.</param>
    public static void RegisterDefaults(IServiceRegistry services, IWindowHost windowHost, PluginManager pluginManager, RuntimeUiComposition uiComposition, RuntimeLoggingComposition loggingComposition) {
        RegisterDefaults(services, windowHost, pluginManager, uiComposition, loggingComposition, new RuntimeShellComposition());
    }

    /// <summary>
    /// Registers the default runtime services for a window host, plugin manager, shared UI composition, shared logging composition, and shared shell composition.
    /// </summary>
    /// <param name="services">The service registry to populate.</param>
    /// <param name="windowHost">The window host used by the runtime.</param>
    /// <param name="pluginManager">The plugin manager participating in startup and shutdown.</param>
    /// <param name="uiComposition">The shared UI composition used by the runtime.</param>
    /// <param name="loggingComposition">The shared logging composition used by the runtime.</param>
    /// <param name="shellComposition">The shared shell composition used by the runtime.</param>
    public static void RegisterDefaults(IServiceRegistry services, IWindowHost windowHost, PluginManager pluginManager, RuntimeUiComposition uiComposition, RuntimeLoggingComposition loggingComposition, RuntimeShellComposition shellComposition) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(windowHost);
        ArgumentNullException.ThrowIfNull(pluginManager);
        ArgumentNullException.ThrowIfNull(uiComposition);
        ArgumentNullException.ThrowIfNull(loggingComposition);
        ArgumentNullException.ThrowIfNull(shellComposition);

        services.Register<IWindowHost>(windowHost);
        services.Register<Kx.Sdk.UI.Actions.IMarkupActionRegistry>(uiComposition.ActionRegistry);
        services.Register<Kx.Sdk.UI.Commands.IUiCommandRegistry>(uiComposition.CommandRegistry);
        services.Register<Kx.Sdk.UI.State.IUiStateStore>(uiComposition.StateStore);
        services.Register<Kx.Sdk.UI.Markup.IControlRegistry>(uiComposition.ControlRegistry);
        services.Register<Kx.Sdk.UI.Themes.IWindowFrameRegistry>(uiComposition.WindowFrameRegistry);
        services.Register<Kx.Sdk.UI.Markup.IWindowContentRegistry>(uiComposition.WindowContentRegistry);

        services.RegisterFactory<Kx.Core.Lifecycle.StartupManager>(Lifetime.Singleton, c => shellComposition.CreateStartupManager(c));
        services.RegisterFactory<Kx.Core.Lifecycle.ShutdownManager>(Lifetime.Singleton, c => shellComposition.CreateShutdownManager(c));
        services.Register(pluginManager);
        services.Register<IShutdownAware>(pluginManager);

        services.Register<ILogSink>(loggingComposition.DebugLogSink);
        services.Register<ILogSink>(loggingComposition.FileLogSink);
        services.RegisterFactory<ILoggerFactory>(Lifetime.Singleton, c => loggingComposition.CreateLoggerFactory(c));
        services.RegisterFactory<ILoggingService>(Lifetime.Singleton, c => loggingComposition.CreateSystemLogger(c));

        services.Register(shellComposition.TrayIcon);
        services.Register<ITrayService, TrayIconService>(Lifetime.Singleton);
    }
}
