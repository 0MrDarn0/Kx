// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.Plugin;
using KUpdater.Backend.WinForms;
using KUpdater.Core.Interop;
using KUpdater.Core.Logging;
using KUpdater.Core.Plugin;
using KUpdater.UI.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace KUpdater;

internal static class Program {
    // Unique name for the mutex — use a GUID or app‑specific ID
    private static readonly string _appMuteName = "Global\\{C0A76B5A-12AB-45C5-B9D9-D693FAA6E7B9}";
    private static Mutex? _mutex;

    [STAThread]
    static void Main() {

        _mutex = new Mutex(initiallyOwned: true, name: _appMuteName, createdNew: out bool createdNew);
        if (!createdNew) {
            BringExistingInstanceToFront();
            return;
        }

        var backend = new WinFormsBackend();
        backend.HandleCreated += (_, _) => {
            // DI Container aufbauen
            var services = new ServiceCollection();

            // TrayIcon Konfiguration (Builder) als Singleton bereitstellen
            var trayConfig = new TrayIcon();

            services.AddSingleton(trayConfig);

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
        GC.KeepAlive(_mutex);
    }

    private static void BringExistingInstanceToFront() {
        try {
            Process current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName)) {
                if (process.Id != current.Id) {
                    IntPtr hWnd = process.MainWindowHandle;
                    if (hWnd != IntPtr.Zero) {
                        // If minimized, restore first
                        if (NativeMethods.IsIconic(hWnd)) {
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                        }

                        // Then bring to front
                        NativeMethods.SetForegroundWindow(hWnd);
                    }
                    break;
                }
            }
        }
        catch {
            // Ignore errors silently
        }
    }
}
