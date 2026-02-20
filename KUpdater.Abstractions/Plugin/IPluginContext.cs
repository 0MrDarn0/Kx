// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.Plugin;

public interface IPluginContext {
    IServiceProvider Services { get; }
    T GetService<T>() where T : notnull;

    IPluginLogger Logger { get; }
}
