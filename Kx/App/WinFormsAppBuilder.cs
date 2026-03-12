// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;

namespace Kx.App;

/// <summary>
/// Builds the bootstrap definition for a WinForms-based Kx application.
/// </summary>
/// <typeparam name="TWindow">The application's main window type.</typeparam>
public sealed class WinFormsAppBuilder<TWindow> where TWindow : Window {
    private Action<IServiceRegistry>? _configureServices;
    private string? _mutexName;

    /// <summary>
    /// Configures the global mutex name used to enforce a single running instance.
    /// </summary>
    /// <param name="mutexName">The mutex name to use.</param>
    /// <returns>The current builder instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mutexName"/> is null, empty, or whitespace.</exception>
    public WinFormsAppBuilder<TWindow> UseMutex(string mutexName) {
        if (string.IsNullOrWhiteSpace(mutexName))
            throw new ArgumentException("The mutex name must not be null or whitespace.", nameof(mutexName));

        _mutexName = mutexName;
        return this;
    }

    /// <summary>
    /// Configures application-specific services before the dependency container is built.
    /// </summary>
    /// <param name="configureServices">The callback used to register application services.</param>
    /// <returns>The current builder instance.</returns>
    public WinFormsAppBuilder<TWindow> ConfigureServices(Action<IServiceRegistry> configureServices) {
        ArgumentNullException.ThrowIfNull(configureServices);

        _configureServices += configureServices;
        return this;
    }

    /// <summary>
    /// Creates the application definition from the current builder state.
    /// </summary>
    /// <returns>The application definition.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no mutex has been configured.</exception>
    public WinFormsAppDefinition<TWindow> Build() {
        if (string.IsNullOrWhiteSpace(_mutexName))
            throw new InvalidOperationException("No mutex has been configured. Call UseMutex(...) before Build().");

        return new WinFormsAppDefinition<TWindow>(_mutexName) {
            ConfigureServices = _configureServices
        };
    }

    /// <summary>
    /// Builds and runs the configured application.
    /// </summary>
    public void Run() {
        WinFormsApp.Run(Build());
    }
}
