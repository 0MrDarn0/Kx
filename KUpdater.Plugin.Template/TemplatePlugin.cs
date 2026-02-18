// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Abstractions.Plugin;

namespace KUpdater.Plugins.Template;

public sealed class TemplatePlugin : IPlugin {
    public string Name => "TemplatePlugin";

    public void Initialize(IPluginContext context) {
        Debug.WriteLine("[TemplatePlugin] Initialized");
    }

    public void Dispose() {
        Debug.WriteLine("[TemplatePlugin] Disposed");
    }
}
