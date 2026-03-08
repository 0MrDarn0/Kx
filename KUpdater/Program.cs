// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Backend.WinForms;
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
        var runtime = new KRuntime(backend);

        backend.HandleCreated += (_, _) => runtime.Start();

        Application.Run(backend);
    }
}
