// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;

namespace Kx.Abstractions.Plugin;

public interface IServicePlugin : IPlugin {
    /// <summary>
    /// Registers plugin services before the dependency container is built.
    /// </summary>
    /// <param name="services">The service registry used by the host.</param>
    void ConfigureServices(IServiceRegistry services);
}
