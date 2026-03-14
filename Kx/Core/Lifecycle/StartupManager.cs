// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Lifecycle;
using Kx.Sdk.Logging;

namespace Kx.Core.Lifecycle;

public sealed class StartupManager(IDependencyContainer container) {
    public async Task StartupAsync() {
        var log = container.Get<ILoggingService>();
        log.Info("Starting up…");

        var services = container.GetAll<IStartupAware>();
        foreach (var service in services) {
            try {
                await service.StartupAsync().ConfigureAwait(false);
            }
            catch (Exception ex) {
                log.Error("Startup error:", ex);
                throw;
            }
        }

        log.Info("Startup complete.");
    }
}
