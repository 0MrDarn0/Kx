// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Logging;
using KUpdater.Abstractions.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace KUpdater.Core.Plugin;

public sealed class PluginContext : IPluginContext {
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public PluginContext(IServiceProvider services, ILogger logger) {
        _services = services;
        _logger = logger;
    }
    public string ApiVersion => HostInfo.ApiVersion;
    public IServiceProvider Services => _services;
    public ILogger Logger => _logger;
    public T GetService<T>() where T : notnull => _services.GetRequiredService<T>();
}
