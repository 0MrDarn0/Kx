// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;
using Kx.Abstractions.Lifecycle;
using Kx.Abstractions.Logging;

namespace Kx.Core.Lifecycle;

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
