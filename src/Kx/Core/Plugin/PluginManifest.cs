// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Plugin;

public sealed class PluginManifest {
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string ApiVersion { get; init; } = "1.0.0";
    public string EntryType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public List<string> Dependencies { get; init; } = new();
}
