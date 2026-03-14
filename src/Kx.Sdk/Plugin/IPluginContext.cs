// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.DI;
using Kx.Sdk.Logging;

namespace Kx.Sdk.Plugin;

public interface IPluginContext {
    string ApiVersion { get; }
    IDependencyContainer Services { get; }
    ILoggingService Logger { get; }
}
