// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using Kx.Utility;
using Kx.WindowHost.WinForms;

namespace Kx;

internal static class Program {
    [STAThread]
    static void Main() {

        const string appMutexName = "Global\\{C0A76B5A-12AB-45C5-B9D9-D693FAA6E7B9}";
        using var instance = AppInstance.Acquire(appMutexName);
        if (instance == null) {
            AppInstance.BringExistingInstanceToFront(Process.GetCurrentProcess().ProcessName);
            return;
        }

        var windowHost = new WinFormsWindowHost();
        var runtime = new Runtime(windowHost);

        windowHost.HandleCreated += (_, _) => runtime.Start();

        Application.Run(windowHost);
    }
}
