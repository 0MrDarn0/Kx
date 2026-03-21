// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;

namespace Kx.Example.App;

internal static class Program {
    [STAThread]
    private static void Main() {
        const string appMutexName = "Global\\{1F2944F2-3EA8-4E65-8F59-2D79B70AB5BE}";

        WinFormsApp
            .Create<MainWindow>()
            .UseMutex(appMutexName)
            .Run();
    }
}
