using Kx.Core.Plugin;

namespace Kx.Tests;

public sealed class PluginDiagnosticsTests {
    [Fact]
    public void WhenTraceIsWrittenThenMessageUsesSharedPluginFormat() {
        var messages = new List<string>();
        var diagnostics = new PluginDiagnostics(traceWriter: messages.Add, errorWriter: _ => { });

        diagnostics.Trace("PluginLoader", "No plugins discovered.");

        Assert.Equal("[PluginLoader] No plugins discovered.", Assert.Single(messages));
    }

    [Fact]
    public void WhenErrorIsWrittenThenExceptionIsIncludedInSharedPluginFormat() {
        var messages = new List<string>();
        var diagnostics = new PluginDiagnostics(traceWriter: _ => { }, errorWriter: messages.Add);
        var exception = new InvalidOperationException("Broken assembly");

        diagnostics.Error("PluginLoader", "Failed to load assembly 'broken.dll'", exception);

        Assert.Contains("[PluginLoader] Failed to load assembly 'broken.dll':", Assert.Single(messages));
    }
}
