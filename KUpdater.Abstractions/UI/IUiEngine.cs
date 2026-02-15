// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.Plugin;

namespace KUpdater.Abstractions.UI;

public interface IUiEngine : IPlugin {
    void BuildUi();
}
