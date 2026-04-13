// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.DI;
using Kx.Sdk.Events;
using Kx.Sdk.WindowHost;

namespace Kx.App;

/// <summary>
/// Coordinates runtime window creation and host event wiring after startup has completed.
/// </summary>
public sealed class RuntimeWindowCoordinator {
    private readonly MsDiContainer _services;
    private readonly IWindowHost _windowHost;
    private Func<Task>? _shutdownAsync;
    private bool _hostClosedHooked;

    /// <summary>
    /// Initializes a new coordinator for window creation against the runtime container and host.
    /// </summary>
    /// <param name="services">The dependency container used to create the runtime window.</param>
    /// <param name="windowHost">The host that shows and closes the runtime window.</param>
    public RuntimeWindowCoordinator(MsDiContainer services, IWindowHost windowHost) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(windowHost);

        _services = services;
        _windowHost = windowHost;
    }

    /// <summary>
    /// Creates the requested window type, wires host shutdown handling, and shows the host window.
    /// </summary>
    /// <param name="windowType">The runtime window type to create.</param>
    /// <param name="shutdownAsync">The callback to invoke when the host closes.</param>
    /// <returns>The created window instance.</returns>
    public Window Show(Type windowType, Func<Task> shutdownAsync) {
        ArgumentNullException.ThrowIfNull(windowType);
        ArgumentNullException.ThrowIfNull(shutdownAsync);

        if (!typeof(Window).IsAssignableFrom(windowType))
            throw new ArgumentException($"The window type must derive from {nameof(Window)}.", nameof(windowType));

        _shutdownAsync = shutdownAsync;

        if (!_hostClosedHooked) {
            _windowHost.Closed += OnHostClosed;
            _hostClosedHooked = true;
        }

        var window = (Window)_services.Create(windowType);
        window.InitializeWindow();
        _windowHost.ShowWindow();
        return window;
    }

    private async void OnHostClosed(ClosedEvent closedEvent) {
        if (_shutdownAsync is null)
            return;

        await _shutdownAsync().ConfigureAwait(false);
    }
}
