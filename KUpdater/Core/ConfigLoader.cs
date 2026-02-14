// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KUpdater.Core;

public static class ConfigLoader {
    public static AppConfig Load(string path) {
        if (!File.Exists(path))
            return new AppConfig();

        var yaml = File.ReadAllText(path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<AppConfig>(yaml)
               ?? new AppConfig();
    }
}
