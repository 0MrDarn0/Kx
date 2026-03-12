// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;

namespace Kx.App;

/// <summary>
/// Describes the bootstrap configuration for a WinForms-based Kx application.
/// </summary>
/// <typeparam name="TWindow">The application's main window type.</typeparam>
public sealed class WinFormsAppDefinition<TWindow> where TWindow : Window {
    /// <summary>
    /// Initializes a new application definition.
    /// </summary>
    /// <param name="mutexName">The global mutex name used to enforce a single running instance.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mutexName"/> is null, empty, or whitespace.</exception>
    public WinFormsAppDefinition(string mutexName) {
        if (string.IsNullOrWhiteSpace(mutexName))
            throw new ArgumentException("The mutex name must not be null or whitespace.", nameof(mutexName));

        MutexName = mutexName;
    }

    /// <summary>
    /// Gets the global mutex name used to enforce a single running instance.
    /// </summary>
    public string MutexName { get; }

    /// <summary>
    /// Gets the application's main window type.
    /// </summary>
    public Type MainWindowType => typeof(TWindow);

    /// <summary>
    /// Gets the application-specific service registration callback.
    /// </summary>
    public Action<IServiceRegistry>? ConfigureServices { get; init; }
}
