// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;
using Kx.Utility;

namespace KxUpdateBuilder;

internal static class Program {
    [STAThread]
    private static void Main() {
        WinFormsApp
            .Create<MainWindow>()
            .UseMutex(GuidMutex.Create(useVersion: false))
            .Run();
    }
}
