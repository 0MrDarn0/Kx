// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Localization;

namespace Kx.Tests;

internal static class TestLanguageBootstrap {
    private static readonly Lock _syncRoot = new();
    private static bool _initialized;

    public static void Initialize() {
        lock (_syncRoot) {
            if (_initialized)
                return;

            string languageFilePath = GetRepoPath("src", "Kx", "Assets", "Languages", "lang_en.yaml");
            IDictionary<string, object> language = LoadLanguageDictionary(languageFilePath);
            LanguageService.Initialize(language, language);
            _initialized = true;
        }
    }

    private static Dictionary<string, object> LoadLanguageDictionary(string filePath) {
        var root = new Dictionary<string, object>(StringComparer.Ordinal);
        var stack = new Stack<(int Indent, Dictionary<string, object> Node)>();
        stack.Push((-1, root));

        foreach (string rawLine in File.ReadLines(filePath)) {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            int indentation = GetIndentation(rawLine);
            string content = rawLine.Trim();
            if (content.StartsWith('#'))
                continue;

            int separatorIndex = content.IndexOf(':');
            if (separatorIndex < 0)
                continue;

            string key = content[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            while (stack.Peek().Indent >= indentation)
                stack.Pop();

            Dictionary<string, object> parent = stack.Peek().Node;
            string valuePortion = content[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(valuePortion)) {
                var child = new Dictionary<string, object>(StringComparer.Ordinal);
                parent[key] = child;
                stack.Push((indentation, child));
                continue;
            }

            parent[key] = Unquote(valuePortion);
        }

        return root;
    }

    private static string Unquote(string value) {
        if (value.Length >= 2) {
            if ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
                return value[1..^1];
        }

        return value;
    }

    private static int GetIndentation(string line) {
        int indentation = 0;
        while (indentation < line.Length && line[indentation] == ' ')
            indentation++;

        return indentation;
    }

    private static string GetRepoPath(params string[] parts) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null) {
            string readmePath = Path.Combine(directory.FullName, "README.md");
            string srcPath = Path.Combine(directory.FullName, "src");
            if (File.Exists(readmePath) && Directory.Exists(srcPath))
                return Path.GetFullPath(Path.Combine(directory.FullName, Path.Combine(parts)));

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
