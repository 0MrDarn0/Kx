// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.DI;
using KUpdater.Abstractions.Logging;
using KUpdater.Abstractions.Plugin;
using KUpdater.Backend.WinForms;
using KUpdater.Core.DI;
using KUpdater.Core.Lifecycle;
using KUpdater.Core.Logging;
using KUpdater.Core.Plugin;
using KUpdater.UI.Platform;
using KUpdater.Utility;

namespace KUpdater;

internal static class Program {
    [STAThread]
    static void Main() {

        const string appMutexName = "Global\\{C0A76B5A-12AB-45C5-B9D9-D693FAA6E7B9}";
        using var instance = AppInstance.Acquire(appMutexName);
        if (instance == null) {
            AppInstance.BringExistingInstanceToFront(Process.GetCurrentProcess().ProcessName);
            return;
        }
        // === Backend initialisieren ===
        var backend = new WinFormsBackend();

        backend.HandleCreated += (_, _) => {

            // === DI-Container initialisieren ===
            var container = new MsDiContainer();

            // Shutdown-Routine
            container.RegisterFactory<ShutdownManager>(
                Lifetime.Singleton,
                c => new ShutdownManager(c));

            // Logging Sinks
            container.RegisterFactory<ILogSink>(
                Lifetime.Singleton,
                c => new AsyncLogSink(new DebugSink()));

            container.RegisterFactory<ILogSink>(
                Lifetime.Singleton,
                c => new AsyncLogSink(
                    new DailyRollingFileSink(
                        5 * 1024 * 1024,
                        5,
                        () => Paths.GetDailyLogFile()
                    )
                )
            );


            // LoggerFactory
            container.RegisterFactory<ILoggerFactory>(
                Lifetime.Singleton,
                c => new LoggerFactory(c)
            );

            // System-Logger
            container.RegisterFactory<ILoggingService>(
                Lifetime.Singleton,
                c => c.Get<ILoggerFactory>().CreateLogger("System")
            );

            // TrayIcon, TrayIconService
            container.Register(new TrayIcon());
            container.Register<ITrayService, TrayIconService>(Lifetime.Singleton);

            // Window
            container.RegisterFactory<Window>(
                Lifetime.Transient,
                c => new Window(backend, c.Get<ITrayService>(), c.Get<ILoggingService>())
            );

            container.Build();


            // === Plugin-System initialisieren ===
            var plugins = PluginLoader.LoadAll<IPlugin>();
            var log = container.Get<ILoggingService>();

            foreach (var plugin in plugins) {
                log.Info($"Loading plugin: {plugin.Name}");
                plugin.Initialize(new PluginContext(container, plugin.Name));
            }

            // Window aus DI holen (erstellt mit backend + tray service)
            var window = container.Get<Window>();

            backend.Shown += (_, _) => window.OnShown();
            backend.FormClosed += async (_, e) => {
                window.OnClosed(e.CloseReason == CloseReason.UserClosing);

                var shutdownManager = container.Get<ShutdownManager>();
                await shutdownManager.ShutdownAsync();
            };
        };

        Application.Run(backend);
    }
}
