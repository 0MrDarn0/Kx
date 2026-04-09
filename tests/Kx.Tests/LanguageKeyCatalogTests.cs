using System.Reflection;

using Kx.Core.Localization;
using KxUpdater;

namespace Kx.Tests;

public sealed class LanguageKeyCatalogTests {
    [Fact]
    public void WhenFrameworkLanguageFileIsGeneratedThenAllYamlLeafKeysExistInTheCatalog() {
        string[] expected = GetYamlLeafKeys(GetRepoPath("src", "Kx", "Assets", "Languages", "lang_en.yaml"));
        string[] actual = GetCatalogKeys(typeof(KxLanguageKeys));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhenUpdaterLanguageFileIsGeneratedThenAllYamlLeafKeysExistInTheCatalog() {
        Type updaterLanguageKeysType = typeof(MainWindow).Assembly.GetType("KxUpdater.UpdaterLanguageKeys", throwOnError: true)!;
        string[] expected = GetYamlLeafKeys(GetRepoPath("apps", "KxUpdater", "Assets", "Languages", "lang_en.yaml"));
        string[] actual = GetCatalogKeys(updaterLanguageKeysType);

        Assert.Equal(expected, actual);
    }

    private static string[] GetCatalogKeys(Type rootType) {
        return EnumerateCatalogKeys(rootType)
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateCatalogKeys(Type type) {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
            if (property.PropertyType != typeof(LanguageKey))
                continue;

            if (property.GetValue(null) is LanguageKey key)
                yield return key.Value;
        }

        foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
            foreach (string key in EnumerateCatalogKeys(nestedType))
                yield return key;
        }
    }

    private static string[] GetYamlLeafKeys(string filePath) {
        var keys = new List<string>();
        var stack = new Stack<(int Indent, string Path)>();
        stack.Push((-1, string.Empty));

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

            string parentPath = stack.Peek().Path;
            string fullPath = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}.{key}";
            string valuePortion = content[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(valuePortion)) {
                stack.Push((indentation, fullPath));
                continue;
            }

            keys.Add(fullPath);
        }

        return keys
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();
    }

    private static int GetIndentation(string line) {
        int indentation = 0;
        while (indentation < line.Length && line[indentation] == ' ')
            indentation++;

        return indentation;
    }

    private static string GetRepoPath(params string[] parts) {
        string path = AppContext.BaseDirectory;
        for (int index = 0; index < 6; index++)
            path = Path.Combine(path, "..");

        return Path.GetFullPath(Path.Combine(path, Path.Combine(parts)));
    }
}
