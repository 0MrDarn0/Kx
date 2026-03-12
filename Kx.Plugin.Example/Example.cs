// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.Plugin;

namespace Kx.Plugin;

public sealed class Example : IPlugin {
    public string Name => "Example";

    public void Initialize(IPluginContext context) {
        context.Logger.Info($"{Name} initialized");
        context.Logger.Info($"ApiVersion: {context.ApiVersion}");
    }

    public void Dispose() {
        // optional cleanup
    }
}
