// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;

namespace KxUpdater {
    internal static class Program {
        [STAThread]
        static void Main() {
            const string appMutexName = "Global\\{5A3635A1-4309-4113-90DD-8099A958D3B2}";

            WinFormsApp
                .Create<MainWindow>()
                .UseMutex(appMutexName)
                .Run();
        }
    }
}
