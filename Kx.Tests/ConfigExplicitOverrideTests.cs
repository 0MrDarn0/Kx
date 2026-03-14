using Kx.Abstractions.UI.Markup;
using Kx.Abstractions.UI.Themes;

namespace Kx.Tests;

public sealed class ConfigExplicitOverrideTests {
    [Fact]
    public void WhenDefaultFramePropertyIsAssignedSchemaDefaultThenItIsTrackedAsExplicit() {
        var config = new DefaultFrameConfig {
            TitleColor = "#F5F5F5"
        };

        Assert.True(config.IsPropertySet(nameof(DefaultFrameConfig.TitleColor)));
    }

    [Fact]
    public void WhenFramePropertyIsAssignedSchemaDefaultThenItIsTrackedAsExplicit() {
        var config = new FrameConfig {
            UseFillColor = false
        };

        Assert.True(config.IsPropertySet(nameof(FrameConfig.UseFillColor)));
    }

    [Fact]
    public void WhenControlNullablePropertyIsAssignedNullThenItIsTrackedAsExplicit() {
        var config = new ControlConfig {
            Text = null
        };

        Assert.True(config.IsPropertySet(nameof(ControlConfig.Text)));
    }
}
