// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KxUpdater.Configuration;

namespace KxUpdater.Tests;

public sealed class AppConfigTests {
    [Fact]
    public void WhenAppConfigIsCreatedThenUpdaterDefaultsAreInitialized() {
        var config = new AppConfig();

        Assert.Equal("https://update.idb-lab.de/", config.Updater.Url);
    }

    [Fact]
    public void WhenAppConfigIsCreatedThenUiDefaultsAreInitialized() {
        var config = new AppConfig();

        Assert.Equal("en", config.Ui.Language);
    }
}
