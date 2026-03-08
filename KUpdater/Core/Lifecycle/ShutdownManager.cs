// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.DI;
using KUpdater.Abstractions.Lifecycle;
using KUpdater.Abstractions.Logging;

namespace KUpdater.Core.Lifecycle;

public sealed class ShutdownManager(IDependencyContainer container) {
    public async Task ShutdownAsync() {
        var log = container.Get<ILoggingService>();
        log.Info("Shutting down…");

        var services = container.GetAll<IShutdownAware>();
        foreach (var service in services) {
            try {
                await service.ShutdownAsync();
            }
            catch (Exception ex) {
                log.Error("Shutdown error:", ex);
            }
        }

        log.Info("Shutdown complete.");
    }
}
