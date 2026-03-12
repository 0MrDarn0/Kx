// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.App;
using Kx.Utility;
using Kx.WindowHost.WinForms;

namespace Kx.Update.App {
    internal static class Program {
        [STAThread]
        static void Main() {

            const string appMutexName = "Global\\{5A3635A1-4309-4113-90DD-8099A958D3B2}";
            using var instance = AppInstance.Acquire(appMutexName);
            if (instance == null) {
                AppInstance.BringExistingInstanceToFront(Process.GetCurrentProcess().ProcessName);
                return;
            }

            // Host erstellen (WinForms Fenster)
            var windowHost = new WinFormsWindowHost();

            // Runtime erstellen
            var runtime = new Runtime(windowHost);

            // App-spezifisches Window registrieren
            runtime.RegisterWindow<MainWindow>();

            // Runtime starten (einmalig!)
            runtime.Start();

            // WinForms Event Loop starten
            Application.Run(windowHost);
        }
    }
}
