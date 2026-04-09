// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.DI;
using Kx.Sdk.DI;
using Kx.Sdk.Logging;
using Kx.Sdk.Plugin;
using Kx.Sdk.UI.Markup;
using Kx.Sdk.UI.Themes;
using Kx.UI.Markup;
using Kx.UI.Themes;

using KxUpdater.Plugin;

namespace Kx.Tests;

public sealed class KxThemeTests {
    [Fact]
    public void WhenKxThemeInitializesThenItRegistersUpdaterFrameTheme() {
        using var testContext = new TestPluginContext();
        var plugin = new KalTheme();

        plugin.Initialize(testContext);

        Assert.True(testContext.ThemeRegistry.TryGet("UpdaterFrame", out var theme));
        Assert.NotNull(theme);
        Assert.Equal("Plugins:KxTheme:Themes:KalOnline:Frame:top_left.png", theme!.Frame.TopLeft);
    }

    [Fact]
    public void WhenKxThemeInitializesThenItRegistersMainWindowDefinition() {
        using var testContext = new TestPluginContext();
        var plugin = new KalTheme();

        plugin.Initialize(testContext);

        Assert.True(testContext.WindowRegistry.TryGet("MainWindow", out var window));
        Assert.NotNull(window);
        Assert.Equal("UpdaterFrame", window!.Theme);
    }

    [Fact]
    public void WhenKxThemeInitializesThenMainWindowContainsContentControls() {
        using var testContext = new TestPluginContext();
        var plugin = new KalTheme();

        plugin.Initialize(testContext);

        Assert.True(testContext.WindowRegistry.TryGet("MainWindow", out var window));
        Assert.NotNull(window);
        Assert.Contains(window!.Controls, control => string.Equals(control.Layer, "Content", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestPluginContext : IPluginContext, IDisposable {
        private readonly MsDiContainer _services = new();

        public TestPluginContext() {
            ThemeRegistry = new ThemeRegistry();
            WindowRegistry = new WindowRegistry();

            _services.Register<IThemeRegistry>(ThemeRegistry);
            _services.Register<IWindowRegistry>(WindowRegistry);
            _services.Register<ILoggingService>(new TestLoggingService());
            _services.Build();
        }

        public string ApiVersion => "1.0.0";
        public IDependencyContainer Services => _services;
        public ILoggingService Logger => _services.Get<ILoggingService>();
        public ThemeRegistry ThemeRegistry { get; }
        public WindowRegistry WindowRegistry { get; }

        public void Dispose() {
        }
    }

    private sealed class TestLoggingService : ILoggingService {
        public void Log(LogLevel level, string message, Exception? ex = null) {
        }

        public void Trace(string message) {
        }

        public void Debug(string message) {
        }

        public void Info(string message) {
        }

        public void Warning(string message) {
        }

        public void Error(string message, Exception? ex = null) {
        }

        public void Critical(string message, Exception? ex = null) {
        }
    }
}
