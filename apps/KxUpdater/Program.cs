// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;
using Kx.Utility;

namespace KxUpdater {
    internal static class Program {
        [STAThread]
        static void Main() {
            WinFormsApp
                .Create<MainWindow>()
                .UseMutex(GuidMutex.Create(useVersion: false))
                .Run();
        }
    }
}
