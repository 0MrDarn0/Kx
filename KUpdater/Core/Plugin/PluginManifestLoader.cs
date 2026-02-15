// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KUpdater.Core.Plugin;

public static class PluginManifestLoader {
    public static PluginManifest Load(string manifestPath) {
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"Manifest not found: {manifestPath}");

        var yaml = File.ReadAllText(manifestPath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<PluginManifest>(yaml)
               ?? throw new InvalidOperationException("Invalid plugin manifest.");
    }
}
