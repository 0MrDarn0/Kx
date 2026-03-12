// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.App;
using Kx.Utility;
using Kx.WindowHost.WinForms;

namespace Kx;

internal static class Program {
    [STAThread]
    static void Main() {

        const string appMutexName = "Global\\{881760AA-0EAA-A241-346D-B62CF504EA9E}";
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
