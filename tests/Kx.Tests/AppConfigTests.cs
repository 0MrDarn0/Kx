using KxUpdater.Configuration;

namespace Kx.Tests;

public sealed class AppConfigTests {
    [Fact]
    public void WhenAppConfigIsCreatedThenUpdaterDefaultsAreInitialized() {
        var config = new AppConfig();

        Assert.Equal("http://webhost.com/KUpdater/", config.Updater.Url);
    }

    [Fact]
    public void WhenAppConfigIsCreatedThenUiDefaultsAreInitialized() {
        var config = new AppConfig();

        Assert.Equal("en", config.Ui.Language);
    }
}
