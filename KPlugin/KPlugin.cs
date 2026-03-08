// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Plugin;

namespace KUpdater.Plugin;

public sealed class KPlugin : IPlugin {
    public string Name => "KPlugin";

    public void Initialize(IPluginContext context) {
        context.Logger.Info($"{Name} initialized");
        context.Logger.Info($"ApiVersion: {context.ApiVersion}");
    }

    public void Dispose() {
        // optional cleanup
    }
}
