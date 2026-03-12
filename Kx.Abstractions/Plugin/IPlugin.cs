// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.Plugin;

public interface IPlugin {
    string Name { get; }
    void Initialize(IPluginContext context);
    public void Dispose();
}
