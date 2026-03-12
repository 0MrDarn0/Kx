// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;
using Kx.Abstractions.Logging;

namespace Kx.Abstractions.Plugin;

public interface IPluginContext {
    string ApiVersion { get; }
    IDependencyContainer Services { get; }
    ILoggingService Logger { get; }
}
