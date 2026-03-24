// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Localization;

namespace KxUpdater;

internal sealed class NewsCoordinator : IDisposable {
    private readonly Func<Action<int>, IDisposable> _subscribeToSelectedIndex;
    private readonly Action<string[]> _setNewsTitles;
    private readonly Action<int> _setSelectedIndex;
    private readonly Action<string> _setChangelogText;
    private IReadOnlyList<NewsEntry> _newsEntries = [];
    private IDisposable? _newsSelectionSubscription;

    public NewsCoordinator(
        Func<Action<int>, IDisposable> subscribeToSelectedIndex,
        Action<string[]> setNewsTitles,
        Action<int> setSelectedIndex,
        Action<string> setChangelogText) {
        ArgumentNullException.ThrowIfNull(subscribeToSelectedIndex);
        ArgumentNullException.ThrowIfNull(setNewsTitles);
        ArgumentNullException.ThrowIfNull(setSelectedIndex);
        ArgumentNullException.ThrowIfNull(setChangelogText);

        _subscribeToSelectedIndex = subscribeToSelectedIndex;
        _setNewsTitles = setNewsTitles;
        _setSelectedIndex = setSelectedIndex;
        _setChangelogText = setChangelogText;
    }

    public void EnsureNewsSelectionBinding() {
        _newsSelectionSubscription ??= _subscribeToSelectedIndex(selectedIndex => {
            if (selectedIndex < 0 || selectedIndex >= _newsEntries.Count)
                return;

            _setChangelogText(_newsEntries[selectedIndex].Content);
        });
    }

    public void ApplyChangelogEntries(string changelogText) {
        _newsEntries = ParseNewsEntries(changelogText);
        _setNewsTitles(_newsEntries.Select(entry => entry.Title).ToArray());

        if (_newsEntries.Count == 0) {
            _setSelectedIndex(-1);
            _setChangelogText(changelogText);
            return;
        }

        _setSelectedIndex(0);
    }

    public void Dispose() {
        _newsSelectionSubscription?.Dispose();
    }

    private static IReadOnlyList<NewsEntry> ParseNewsEntries(string changelogText) {
        if (string.IsNullOrWhiteSpace(changelogText))
            return [];

        string normalizedText = changelogText.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = normalizedText.Split('\n');
        List<NewsEntry> entries = [];
        List<string> currentContent = [];
        string? currentTitle = null;

        foreach (string rawLine in lines) {
            string line = rawLine.TrimEnd();
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("#", StringComparison.Ordinal)) {
                AddNewsEntry(entries, currentTitle, currentContent);
                currentTitle = trimmedLine.TrimStart('#', ' ');
                currentContent.Clear();
                continue;
            }

            if (currentTitle is null && !string.IsNullOrWhiteSpace(trimmedLine))
                currentTitle = trimmedLine.Length <= 48 ? trimmedLine : trimmedLine[..48] + "...";

            currentContent.Add(line);
        }

        AddNewsEntry(entries, currentTitle, currentContent);

        if (entries.Count != 0)
            return entries;

        return [new NewsEntry(LanguageService.Translate("info.news_latest"), changelogText)];
    }

    private static void AddNewsEntry(List<NewsEntry> entries, string? title, List<string> contentLines) {
        string content = string.Join(Environment.NewLine, contentLines).Trim();
        if (string.IsNullOrWhiteSpace(content))
            return;

        string resolvedTitle = string.IsNullOrWhiteSpace(title)
            ? content.Split(Environment.NewLine, 2, StringSplitOptions.None)[0].Trim()
            : title.Trim();

        entries.Add(new NewsEntry(resolvedTitle, content));
    }

    private sealed record NewsEntry(string Title, string Content);
}
