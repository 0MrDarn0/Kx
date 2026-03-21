using Kx.Core.Plugin;

namespace Kx.Tests;

public sealed class PluginCompatibilityPolicyTests {
    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("1.1.0", true)]
    [InlineData("1.2.0", false)]
    [InlineData("2.0.0", false)]
    [InlineData("invalid", false)]
    public void WhenManifestApiVersionIsEvaluatedThenCompatibilityMatchesHostPolicy(string apiVersion, bool expected) {
        var policy = new PluginCompatibilityPolicy("1.1.0");
        var manifest = new PluginManifest {
            Name = "Sample",
            ApiVersion = apiVersion
        };

        var isCompatible = policy.IsCompatible(manifest);

        Assert.Equal(expected, isCompatible);
    }
}

public sealed class PluginDependencyResolverTests {
    [Fact]
    public void WhenDependenciesExistThenLoadOrderPlacesDependenciesFirst() {
        var resolver = new PluginDependencyResolver();
        var plugins = new Dictionary<string, PluginManifest>(StringComparer.OrdinalIgnoreCase) {
            ["Shell"] = new PluginManifest {
                Name = "Shell",
                Dependencies = ["Core"]
            },
            ["Core"] = new PluginManifest {
                Name = "Core"
            }
        };

        var loadOrder = resolver.ResolveLoadOrder(plugins);

        Assert.Equal(["Core", "Shell"], loadOrder);
    }

    [Fact]
    public void WhenDependencyIsMissingThenResolveLoadOrderThrows() {
        var resolver = new PluginDependencyResolver();
        var plugins = new Dictionary<string, PluginManifest>(StringComparer.OrdinalIgnoreCase) {
            ["Shell"] = new PluginManifest {
                Name = "Shell",
                Dependencies = ["Core"]
            }
        };

        var action = () => resolver.ResolveLoadOrder(plugins);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("Missing dependency 'Core' for plugin 'Shell'.", exception.Message);
    }

    [Fact]
    public void WhenDependenciesAreCyclicThenResolveLoadOrderThrows() {
        var resolver = new PluginDependencyResolver();
        var plugins = new Dictionary<string, PluginManifest>(StringComparer.OrdinalIgnoreCase) {
            ["Shell"] = new PluginManifest {
                Name = "Shell",
                Dependencies = ["Core"]
            },
            ["Core"] = new PluginManifest {
                Name = "Core",
                Dependencies = ["Shell"]
            }
        };

        var action = () => resolver.ResolveLoadOrder(plugins);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("Cyclic dependency detected at 'Shell'.", exception.Message);
    }
}
