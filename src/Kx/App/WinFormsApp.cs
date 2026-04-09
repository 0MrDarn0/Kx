// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.Core.Configuration;
using Kx.Core.Localization;
using Kx.Utility;
using Kx.WindowHost.WinForms;

namespace Kx.App;

public static class WinFormsApp {
    /// <summary>
    /// Creates a builder for a WinForms-based Kx application.
    /// </summary>
    /// <typeparam name="TWindow">The application's main window type.</typeparam>
    /// <returns>A builder that can be used to configure and run the application.</returns>
    public static WinFormsAppBuilder<TWindow> Create<TWindow>() where TWindow : Window {
        return new WinFormsAppBuilder<TWindow>();
    }

    /// <summary>
    /// Boots and runs a WinForms-based Kx application with the specified main window.
    /// </summary>
    /// <param name="mutexName">The global mutex name used to enforce a single running instance.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mutexName"/> is null, empty, or whitespace.</exception>
    public static void Run<TWindow>(string mutexName) where TWindow : Window {
        Run(new WinFormsAppDefinition<TWindow>(mutexName));
    }

    /// <summary>
    /// Boots and runs a WinForms-based Kx application from a definition object.
    /// </summary>
    /// <typeparam name="TWindow">The application's main window type.</typeparam>
    /// <param name="definition">The application bootstrap definition.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition"/> is null.</exception>
    public static void Run<TWindow>(WinFormsAppDefinition<TWindow> definition) where TWindow : Window {
        ArgumentNullException.ThrowIfNull(definition);

        GlobalExceptionHandler.Register();
        EnsureLocalizationLoaded();

        using var instance = AppInstance.Acquire(definition.MutexName);
        if (instance == null) {
            // Another instance is running - try to bring it to front
            bool broughtToFront = AppInstance.BringExistingInstanceToFront(Process.GetCurrentProcess().ProcessName);

            if (!broughtToFront) {
                // Check if there are zombie processes
                var zombieProcessIds = AppInstance.FindZombieProcesses(Process.GetCurrentProcess().ProcessName);
                string message = CreateSingleInstanceMessage(zombieProcessIds);

                MessageBox.Show(
                    message,
                    LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.Title),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return;
        }

        var windowHost = new WinFormsWindowHost();
        var runtime = new Runtime(windowHost);

        runtime.RegisterWindow(definition.MainWindowType);

        if (definition.ConfigureServices is not null)
            runtime.ConfigureServices(definition.ConfigureServices);

        windowHost.HandleCreated += async (_, _) => {
            try {
                await runtime.StartAsync();
            }
            catch (Exception ex) {
                Application.OnThreadException(ex);
            }
        };

        Application.Run(windowHost);
    }

    private static void EnsureLocalizationLoaded() {
        string languageCode = ConfigLoader.Load<RuntimeConfig>(Paths.GetConfig("app.yaml")).Ui.Language;
        LanguageLoader.Load(languageCode);
    }

    private static string CreateSingleInstanceMessage(IReadOnlyCollection<int> zombieProcessIds) {
        ArgumentNullException.ThrowIfNull(zombieProcessIds);

        return zombieProcessIds.Count > 0
            ? LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.ZombieMessage, string.Join(", ", zombieProcessIds))
            : LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.BackgroundMessage);
    }
}
