// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Localization;

public static class LanguageService {
    private static IDictionary<string, object>? _lang;
    private static IDictionary<string, object>? _fallback;

    public static void Initialize(IDictionary<string, object> lang, IDictionary<string, object> fallback) {
        LanguageService._lang = lang;
        LanguageService._fallback = fallback;
    }

    /// <summary>
    /// Resolves a localized string for the specified strongly typed key.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="args">Optional format arguments.</param>
    public static string Translate(LanguageKey key, params object[] args) {
        return Translate(key.Value, args);
    }

    public static string Translate(string key, params object[] args) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string raw = ResolveRaw(key, returnKeyWhenUninitialized: true) ?? $"[MISSING:{key}]";
        return Format(raw, args);
    }

    /// <summary>
    /// Attempts to resolve a localized string for the specified strongly typed key.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="value">The resolved string when the lookup succeeds.</param>
    public static bool TryTranslate(LanguageKey key, out string value) {
        return TryTranslate(key.Value, out value);
    }

    /// <summary>
    /// Attempts to resolve a localized string for the specified localization id.
    /// </summary>
    /// <param name="key">The dotted localization id.</param>
    /// <param name="value">The resolved string when the lookup succeeds.</param>
    public static bool TryTranslate(string key, out string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string? raw = ResolveRaw(key, returnKeyWhenUninitialized: false);
        value = raw ?? string.Empty;
        return raw is not null;
    }

    private static string? ResolveRaw(string key, bool returnKeyWhenUninitialized) {
        if (_lang == null || _fallback == null)
            return returnKeyWhenUninitialized ? key : null;

        return Lookup(_lang, key) ?? Lookup(_fallback, key);
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
