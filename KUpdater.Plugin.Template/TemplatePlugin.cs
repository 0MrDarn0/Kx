// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Plugin;

namespace KUpdater.Plugin.Template;

public sealed class TemplatePlugin : IPlugin {
    public string Name => "TemplatePlugin";

    public void Initialize(IPluginContext context) {

        if (context.ApiVersion.StartsWith("1.2")) {
            context.Logger.Info($"Host ApiVersion: {context.ApiVersion}");
        }
    }

    public void Dispose() {

    }
}
