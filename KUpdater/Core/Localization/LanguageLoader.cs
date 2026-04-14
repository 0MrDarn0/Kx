// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections;
using KUpdater.Utility;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KUpdater.Core.Localization;

public static class LanguageLoader {
    public static void Load(string code, string fallback = "en") {
        var langDict = LoadYamlToDictionary(Paths.GetLang(code));
        var fallbackDict = LoadYamlToDictionary(Paths.GetLang(fallback));
        LanguageService.Initialize(langDict, fallbackDict);
    }

    private static IDictionary<string, object> LoadYamlToDictionary(string path) {
        if (!File.Exists(path))
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var yaml = File.ReadAllText(path);

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
                var key = kv.Key?.ToString() ?? "";
                result[key] = ConvertNode(kv.Value);
            }
        } else if (node is IDictionary<string, object> mapStr) {
            foreach (var kv in mapStr) {
                result[kv.Key] = ConvertNode(kv.Value);
            }
        }

        return result;
    }

    private static object ConvertNode(object? node) {
        if (node is IDictionary<object, object> dictObj) {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dictObj) {
                dict[kv.Key?.ToString() ?? ""] = ConvertNode(kv.Value);
            }
            return dict;
        }

        if (node is IDictionary<string, object> dictStr) {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dictStr) {
                dict[kv.Key] = ConvertNode(kv.Value);
            }
            return dict;
        }

        if (node is IList list) {
            var outList = new List<object>();
            foreach (var item in list)
                outList.Add(ConvertNode(item));
            return outList;
        }

        // Scalar (string, int, bool, ...)
        return node?.ToString() ?? "";
    }
}
