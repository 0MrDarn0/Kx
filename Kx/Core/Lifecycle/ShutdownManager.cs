// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Lifecycle;
using Kx.Sdk.Logging;

namespace Kx.Core.Lifecycle;

public sealed class ShutdownManager(IDependencyContainer container) {
    public async Task ShutdownAsync() {
        var log = container.Get<ILoggingService>();
        log.Info("Shutting down…");

        var services = container.GetAll<IShutdownAware>();
        foreach (var service in services) {
            try {
                await service.ShutdownAsync().ConfigureAwait(false);
            }
            catch (Exception ex) {
                log.Error("Shutdown error:", ex);
            }
        }

        log.Info("Shutdown complete.");
    }
}
