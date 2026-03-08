// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.DI;
using KUpdater.Abstractions.Plugin;
using KUpdater.Backend.WinForms;
using KUpdater.Core.DI;
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

        var backend = new WinFormsBackend();

        backend.HandleCreated += (_, _) => {

            // ============================================================
            // DI-Container initialisieren
            // ============================================================
            var container = new MsDiContainer();

            // TrayIcon als Singleton-Instanz
            container.Register(new TrayIcon());

            // TrayService als Singleton
            container.Register<ITrayService, TrayIconService>(Lifetime.Singleton);

            // Window als Transient (mit Factory, da Parameter)
            container.RegisterFactory<Window>(
                Lifetime.Transient,
                dc => new Window(backend, dc.Get<ITrayService>())
                );

            // Container final bauen
            container.Build();


            // === Plugin-System initialisieren ===
            var plugins = PluginLoader.LoadAll<IPlugin>();
            foreach (var plugin in plugins) {
                Debug.WriteLine($"Loading plugin: {plugin.Name}");
                var logger = new Logger(plugin.Name);
                var context = new PluginContext(container, logger);
                plugin.Initialize(context);
            }
            // ====================================

            // Window aus DI holen (erstellt mit backend + tray service)
            var window = container.Get<Window>();

            backend.Shown += (_, _) => window.OnShown();
            backend.FormClosed += (_, e) => window.OnClosed(e.CloseReason == CloseReason.UserClosing);
        };

        Application.Run(backend);
    }
}
