// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Plugin;

public sealed class PluginManifest {
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string ApiVersion { get; set; } = "";
    public string EntryType { get; set; } = "";
    public string? Description { get; set; }
    public string? Author { get; set; }
    public List<string>? Dependencies { get; set; }
}
