// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Logging;

namespace KUpdater.Abstractions.Plugin;

public interface IPluginContext {
    string ApiVersion { get; }
    IServiceProvider Services { get; }
    T GetService<T>() where T : notnull;
    ILogger Logger { get; }
}
