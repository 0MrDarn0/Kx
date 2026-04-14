using Kx.Core.Localization;
using Kx.Utility;

namespace Kx.Tests;

public sealed class LanguageLoaderTests {
    [Fact]
    public void WhenAppLanguageIsMissingThenFrameworkSingleInstanceDialogTextsComeFromEmbeddedDefaults() {
        Directory.CreateDirectory(Paths.LangFolder);

        LanguageLoader.Load($"missing-{Guid.NewGuid():N}", $"missing-{Guid.NewGuid():N}");

        Assert.Equal("Application Already Running", LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.Title));
        Assert.Equal(
            "Another application instance is already running but has no visible window.\n\nDetected process IDs: 11, 42\n\nPossible causes:\n- The previous instance crashed during initialization\n- The application is hung without a visible window\n\nResolution:\n1. Open Task Manager (Ctrl+Shift+Esc)\n2. End the listed processes\n3. Start the application again",
            LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.ZombieMessage, "11, 42"));
    }

    [Fact]
    public void WhenAppLanguageOverridesSingleInstanceDialogTextThenOverrideIsReturned() {
        Directory.CreateDirectory(Paths.LangFolder);

        string languageCode = $"override-{Guid.NewGuid():N}";
        File.WriteAllText(Paths.GetLang(languageCode),
            """
            dialog:
              single_instance:
                title: "Launcher is already active"
            """);

        LanguageLoader.Load(languageCode, $"missing-{Guid.NewGuid():N}");

        Assert.Equal("Launcher is already active", LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.Title));
        Assert.Equal(
            "Another application instance is already running but has no visible window.\n\nPossible causes:\n- The application is still starting (please wait)\n- The previous instance is running in the background\n\nEnd all running instances in Task Manager and try again.",
            LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.BackgroundMessage));
    }

    [Fact]
    public void WhenGeneratedFrameworkKeyUsesNormalizedTokensThenItTranslatesCorrectly() {
        Directory.CreateDirectory(Paths.LangFolder);

        LanguageLoader.Load($"missing-{Guid.NewGuid():N}", $"missing-{Guid.NewGuid():N}");

        Assert.Equal("Downloading update package...", LanguageService.Translate(KxLanguageKeys.Status.DownloadingPackage));
    }

    [Fact]
    public void WhenTypedLanguageKeyIsUsedThenItTranslatesLikeTheStringKey() {
        Directory.CreateDirectory(Paths.LangFolder);

        string languageCode = $"typed-{Guid.NewGuid():N}";
        File.WriteAllText(Paths.GetLang(languageCode),
            """
            status:
              website_opening: "Opening website..."
            """);

        LanguageLoader.Load(languageCode, $"missing-{Guid.NewGuid():N}");

        Assert.Equal("Opening website...", LanguageService.Translate(new LanguageKey("status.website_opening")));
    }

    [Fact]
    public void WhenTypedKeyExistsThenTryTranslateReturnsTrue() {
        Directory.CreateDirectory(Paths.LangFolder);

        LanguageLoader.Load($"missing-{Guid.NewGuid():N}", $"missing-{Guid.NewGuid():N}");

        bool translated = LanguageService.TryTranslate(KxLanguageKeys.Dialog.SingleInstance.Title, out string value);

        Assert.True(translated);
        Assert.Equal("Application Already Running", value);
    }

    [Fact]
    public void WhenTypedKeyIsMissingThenTryTranslateReturnsFalse() {
        Directory.CreateDirectory(Paths.LangFolder);

        LanguageLoader.Load($"missing-{Guid.NewGuid():N}", $"missing-{Guid.NewGuid():N}");

        bool translated = LanguageService.TryTranslate(new LanguageKey("missing.section.key"), out string value);

        Assert.False(translated);
        Assert.Equal(string.Empty, value);
    }
}
