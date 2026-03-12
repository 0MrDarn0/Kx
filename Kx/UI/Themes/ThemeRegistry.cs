// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.UI.Themes;

public sealed class ThemeRegistry : IThemeRegistry {
    private readonly Dictionary<string, WindowTheme> _themes = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, WindowTheme theme) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(theme);

        _themes[name] = theme;
    }

    public bool TryGet(string name, out WindowTheme? theme) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _themes.TryGetValue(name, out theme);
    }
}
