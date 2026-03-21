// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Plugin;

public sealed record PluginCatalogEntry(
    string Name,
    string Folder,
    PluginManifest Manifest,
    string? DllPath
);
