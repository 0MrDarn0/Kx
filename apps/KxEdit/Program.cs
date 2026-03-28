// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;

namespace KxEdit {
    internal static class Program {
        [STAThread]
        static void Main() {
            const string appMutexName = "Global\\{A1B2C3D4-E5F6-7890-1234-56789ABCDEF0}";

            WinFormsApp
                .Create<MainWindow>()
                .UseMutex(appMutexName)
                .Run();
        }
    }
}
