// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kx.Core.Configuration;

public static class ConfigLoader {
    public static T Load<T>(string path) where T : new() {
        if (!File.Exists(path))
            return new T();

        var yaml = File.ReadAllText(path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<T>(yaml) ?? new T();
    }
}
