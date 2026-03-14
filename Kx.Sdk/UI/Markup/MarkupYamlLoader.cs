// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kx.Sdk.UI.Markup;

/// <summary>
/// Loads markup-related configuration objects from YAML files.
/// </summary>
public static class MarkupYamlLoader {
    public static T Load<T>(string path) where T : new() {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"The markup YAML file '{path}' could not be found.", path);

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<T>(yaml) ?? new T();
    }
}
