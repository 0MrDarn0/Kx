// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.UI.Themes;

/// <summary>
/// Registers named window themes that can be applied by windows and plugins.
/// </summary>
public interface IThemeRegistry {
    void Register(string name, WindowTheme theme);
    bool TryGet(string name, out WindowTheme? theme);
}
