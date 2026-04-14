using Kx.Sdk.UI.Binding;

namespace Kx.Tests;

public sealed class UiBindingExpressionTests {
    [Fact]
    public void WhenPlainPathSpecifiedThenParsesWithoutConverters() {
        var result = UiBindingExpression.TryParse("example.title", out var binding);

        Assert.True(result);
        Assert.NotNull(binding);
        Assert.Equal("example.title", binding!.Path);
        Assert.Empty(binding.Converters);
    }

    [Fact]
    public void WhenConverterPipelineSpecifiedThenParsesConvertersInOrder() {
        UiBindingExpression.TryParse("example.title|trim|upper|prefix:[BOUND] ", out var binding);

        Assert.NotNull(binding);
        Assert.Collection(binding!.Converters,
            converter => Assert.Equal("trim", converter.Name),
            converter => Assert.Equal("upper", converter.Name),
            converter => {
                Assert.Equal("prefix", converter.Name);
                Assert.Equal("[BOUND]", converter.Argument);
            });
    }

    [Fact]
    public void WhenConverterSegmentIsInvalidThenParsingFails() {
        var result = UiBindingExpression.TryParse("example.title|:broken", out _);

        Assert.False(result);
    }
}
