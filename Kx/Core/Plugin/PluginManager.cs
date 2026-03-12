// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;
using Kx.Abstractions.Lifecycle;
using Kx.Abstractions.Logging;
using Kx.Abstractions.Plugin;

namespace Kx.Core.Plugin;

public sealed class PluginManager(IDependencyContainer services) : IShutdownAware {
    private bool _initialized;

    public void InitializeAll() {
        if (_initialized)
            return;

        _initialized = true;

        var log = services.Get<ILoggingService>();
        var plugins = PluginLoader.LoadAll<IPlugin>();

        foreach (var plugin in plugins) {
            try {
                log.Info($"Loading plugin: {plugin.Name}");
                plugin.Initialize(new PluginContext(services, plugin.Name));
            }
            catch (Exception ex) {
                log.Error($"Plugin initialization failed: {plugin.Name}", ex);
                PluginRegistry.Unload(plugin);
            }
        }
    }

    public ValueTask ShutdownAsync() {
        var log = services.Get<ILoggingService>();

        foreach (var plugin in PluginRegistry.GetUnloadOrder()) {
            try {
                log.Info($"Unloading plugin: {plugin.Name}");
                PluginRegistry.Unload(plugin);
            }
            catch (Exception ex) {
                log.Error($"Plugin unload failed: {plugin.Name}", ex);
            }
        }

        return ValueTask.CompletedTask;
    }
}
