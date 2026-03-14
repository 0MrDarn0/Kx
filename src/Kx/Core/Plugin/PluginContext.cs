// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Logging;
using Kx.Sdk.Plugin;

namespace Kx.Core.Plugin;

public sealed class PluginContext(IDependencyContainer services, string pluginName) : IPluginContext {
    public string ApiVersion => HostInfo.ApiVersion;
    public IDependencyContainer Services { get; } = services;
    public ILoggingService Logger { get; } = services.Get<ILoggerFactory>().CreateLogger(pluginName);
}
