// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.DI;
using KUpdater.Abstractions.Logging;
using KUpdater.Abstractions.Plugin;

namespace KUpdater.Core.Plugin;

public sealed class PluginContext(IDependencyContainer services, ILogger logger) : IPluginContext {
    public string ApiVersion => HostInfo.ApiVersion;
    public IDependencyContainer Services { get; } = services;
    public ILogger Logger { get; } = logger;
}
