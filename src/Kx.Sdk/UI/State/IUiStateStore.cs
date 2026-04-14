// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.State;

/// <summary>
/// Stores UI state values and notifies bindings when a state path changes.
/// </summary>
public interface IUiStateStore {
    /// <summary>
    /// Sets the current value for a state path.
    /// </summary>
    void Set(string path, object? value);

    /// <summary>
    /// Sets the current value for a typed state key.
    /// </summary>
    void Set<T>(UiStateKey<T> key, T? value);

    /// <summary>
    /// Tries to get the current raw value for a state path.
    /// </summary>
    bool TryGet(string path, out object? value);

    /// <summary>
    /// Tries to get the current raw value for a typed state key.
    /// </summary>
    bool TryGet<T>(UiStateKey<T> key, out T? value);

    /// <summary>
    /// Tries to get the current value for a state path as the requested type.
    /// </summary>
    bool TryGet<T>(string path, out T? value);

    /// <summary>
    /// Subscribes to changes for a state path.
    /// </summary>
    IDisposable Subscribe(string path, Action<object?> listener);

    /// <summary>
    /// Subscribes to changes for a typed state key.
    /// </summary>
    IDisposable Subscribe<T>(UiStateKey<T> key, Action<T?> listener);
}
