using Kx.Utility;

using SkiaSharp;

namespace Kx.Tests;

public sealed class FileResourceProviderTests {
    [Fact]
    public void WhenFontResourceIsMissingThenTryGetSkiaTypefaceReturnsNull() {
        using FileResourceProvider provider = new(Path.GetTempPath());

        var typeface = provider.TryGetSkiaTypeface($"missing-{Guid.NewGuid():N}.ttf");

        Assert.Null(typeface);
    }

    [Fact]
    public void WhenFontResourceWasLoadedThenSubsequentTypefaceRequestsUseCachedData() {
        string fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string sourceFont = Directory.GetFiles(fontsDirectory, "*.ttf").First();
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"kx-font-cache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string targetFont = Path.Combine(tempDirectory, Path.GetFileName(sourceFont));
        File.Copy(sourceFont, targetFont);

        using FileResourceProvider provider = new(tempDirectory);
        SKTypeface? firstTypeface = provider.TryGetSkiaTypeface(Path.GetFileName(targetFont));
        File.Delete(targetFont);
        SKTypeface? secondTypeface = provider.TryGetSkiaTypeface(Path.GetFileName(targetFont));

        try {
            Assert.NotNull(firstTypeface);
            Assert.NotNull(secondTypeface);
            Assert.NotSame(firstTypeface, secondTypeface);
        }
        finally {
            firstTypeface?.Dispose();
            secondTypeface?.Dispose();
        }
    }
}
