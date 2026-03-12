// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kx.Core.Plugin;

public static class PluginManifestLoader {
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static PluginManifest Load(string path) {
        try {
            var yaml = File.ReadAllText(path);
            var manifest = _deserializer.Deserialize<PluginManifest>(yaml);
            return manifest ?? throw new InvalidOperationException("Manifest deserialized to null.");
        }
        catch (Exception ex) {
            Debug.WriteLine($"[PluginManifestLoader] Failed to load '{path}': {ex.Message}");
            throw;
        }
    }
}
