using Kx.Sdk.UI.Markup;

namespace Kx.Tests;

public sealed class MarkupYamlLoaderTests {
    [Fact]
    public void WhenFontDefinesResourceThenYamlLoaderPopulatesIt() {
        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.yaml");

        File.WriteAllText(tempFile, """
font:
  name: "Custom"
  size: 16
  style: "Regular"
  resource: "Plugins:KalTheme:Fonts:main.ttf"
""");

        FontConfigContainer result = MarkupYamlLoader.Load<FontConfigContainer>(tempFile);

        Assert.Equal("Plugins:KalTheme:Fonts:main.ttf", result.Font?.Resource);
    }

    private sealed class FontConfigContainer {
        public FontConfig? Font { get; set; }
    }
}
