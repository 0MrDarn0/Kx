// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.Plugin;
using KUpdater.Backend.WinForms;
using KUpdater.Core.Logging;
using KUpdater.Core.Plugin;
using KUpdater.UI.Platform;
using KUpdater.Utility;
using Microsoft.Extensions.DependencyInjection;

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
            // DI Container aufbauen
            var services = new ServiceCollection();

            // TrayIcon Konfiguration (Builder) als Singleton bereitstellen
            services.AddSingleton(new TrayIcon());

            // TrayService registrieren
            services.AddSingleton<ITrayService, TrayIconService>();

            // Falls Window selbst DI benötigt, registriere es als Factory
            services.AddTransient(sp => new Window(backend, sp.GetRequiredService<ITrayService>()));

            var serviceProvider = services.BuildServiceProvider();

            // === Plugin-System initialisieren ===
            var plugins = PluginLoader.LoadAll<IPlugin>();
            foreach (var plugin in plugins) {
                Debug.WriteLine($"Loading plugin: {plugin.Name}");
                var logger = new Logger(plugin.Name);
                var context = new PluginContext(serviceProvider, logger);
                plugin.Initialize(context);
            }
            // ====================================

            // Window aus DI holen (erstellt mit backend + tray service)
            var window = serviceProvider.GetRequiredService<Window>();

            backend.Shown += (_, _) => window.OnShown();
            backend.FormClosed += (_, e) => window.OnClosed(e.CloseReason == CloseReason.UserClosing);
        };

        Application.Run(backend);
    }
}
