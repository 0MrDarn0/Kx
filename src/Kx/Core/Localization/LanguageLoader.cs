// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections;
using Kx.Utility;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kx.Core.Localization;

public static class LanguageLoader {
    private const string DefaultLanguageCode = "en";
    private const string EmbeddedLanguagePrefix = "Kx.Assets.Languages.lang_";
    private const string EmbeddedLanguageExtension = ".yaml";

    public static void Load(string code, string fallback = DefaultLanguageCode) {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Language code cannot be null or whitespace.", nameof(code));

        if (string.IsNullOrWhiteSpace(fallback))
            throw new ArgumentException("Fallback language code cannot be null or whitespace.", nameof(fallback));

        var libraryFallback = LoadEmbeddedYamlToDictionary(DefaultLanguageCode);
        var appFallback = LoadYamlToDictionary(Paths.GetLang(fallback));
        var appLanguage = LoadYamlToDictionary(Paths.GetLang(code));

        var effectiveFallback = MergeDictionaries(libraryFallback, appFallback);
        var effectiveLanguage = MergeDictionaries(effectiveFallback, appLanguage);

        LanguageService.Initialize(effectiveLanguage, effectiveFallback);
    }

    private static IDictionary<string, object> LoadYamlToDictionary(string path) {
        if (!File.Exists(path))
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var yaml = File.ReadAllText(path);
        return DeserializeYaml(yaml);
    }

    private static IDictionary<string, object> LoadEmbeddedYamlToDictionary(string code) {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var resourceName = $"{EmbeddedLanguagePrefix}{code}{EmbeddedLanguageExtension}";
        var assembly = typeof(LanguageLoader).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(stream);
        var yaml = reader.ReadToEnd();
        return DeserializeYaml(yaml);
    }

    private static IDictionary<string, object> DeserializeYaml(string yaml) {
        ArgumentNullException.ThrowIfNull(yaml);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var obj = deserializer.Deserialize<object>(yaml);
        return ConvertToDictionary(obj);
    }

    private static IDictionary<string, object> ConvertToDictionary(object? node) {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (node is IDictionary<object, object> map) {
            foreach (var kv in map) {
                var key = kv.Key?.ToString() ?? string.Empty;
                result[key] = ConvertNode(kv.Value);
            }
        }
        else if (node is IDictionary<string, object> mapStr) {
            foreach (var kv in mapStr)
                result[kv.Key] = ConvertNode(kv.Value);
        }

        return result;
    }

    private static object ConvertNode(object? node) {
        if (node is IDictionary<object, object> dictObj) {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dictObj)
                dict[kv.Key?.ToString() ?? string.Empty] = ConvertNode(kv.Value);
            return dict;
        }

        if (node is IDictionary<string, object> dictStr) {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dictStr)
                dict[kv.Key] = ConvertNode(kv.Value);
            return dict;
        }

        if (node is IList list) {
            var outList = new List<object>();
            foreach (var item in list)
                outList.Add(ConvertNode(item));
            return outList;
        }

        return node?.ToString() ?? string.Empty;
    }

    private static IDictionary<string, object> MergeDictionaries(IDictionary<string, object> baseValues, IDictionary<string, object> overrideValues) {
        ArgumentNullException.ThrowIfNull(baseValues);
        ArgumentNullException.ThrowIfNull(overrideValues);

        var result = CloneDictionary(baseValues);

        foreach (var entry in overrideValues) {
            if (result.TryGetValue(entry.Key, out var baseNode)
                && baseNode is IDictionary<string, object> baseMap
                && entry.Value is IDictionary<string, object> overrideMap) {
                result[entry.Key] = MergeDictionaries(baseMap, overrideMap);
                continue;
            }

            result[entry.Key] = CloneNode(entry.Value);
        }

        return result;
    }

    private static Dictionary<string, object> CloneDictionary(IDictionary<string, object> source) {
        var clone = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in source)
            clone[entry.Key] = CloneNode(entry.Value);

        return clone;
    }

    private static object CloneNode(object value) {
        if (value is IDictionary<string, object> dict)
            return CloneDictionary(dict);

        if (value is IList list) {
            var copy = new List<object>(list.Count);
            foreach (var item in list)
                copy.Add(item is null ? string.Empty : CloneNode(item));
            return copy;
        }

        return value;
    }
}
