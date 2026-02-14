// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)
namespace KUpdater.Core.Localization;

public static class LanguageService {
    private static IDictionary<string, object>? Lang;
    private static IDictionary<string, object>? Fallback;

    public static void Initialize(IDictionary<string, object> lang, IDictionary<string, object> fallback) {
        LanguageService.Lang = lang;
        LanguageService.Fallback = fallback;
    }

    public static string Translate(string key, params object[] args) {
        if (Lang == null || Fallback == null)
            return Format(key, args);

        string? raw = Lookup(Lang, key) ?? Lookup(Fallback, key);

        if (raw == null)
            raw = $"[MISSING:{key}]";

        return Format(raw, args);
    }

    private static string? Lookup(IDictionary<string, object> root, string key) {
        object? node = root;
        foreach (var part in key.Split('.')) {
            if (node is IDictionary<string, object> dict) {
                if (!dict.TryGetValue(part, out node))
                    return null;
            } else {
                return null;
            }
        }

        return node as string;
    }

    private static string Format(string raw, object[] args) =>
        args.Length > 0 ? string.Format(raw, args) : raw;
}
