// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;

using Kx.Abstractions.UI.State;

namespace Kx.UI.State;

public sealed class UiStateStore : IUiStateStore {
    private readonly ConcurrentDictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<Action<object?>>> _listeners = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string path, object? value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _values[path] = value;

        List<Action<object?>> listeners;
        lock (_gate) {
            if (!_listeners.TryGetValue(path, out var registered) || registered.Count == 0)
                return;

            listeners = [.. registered];
        }

        foreach (var listener in listeners)
            listener(value);
    }

    public void Set<T>(UiStateKey<T> key, T? value) {
        ArgumentNullException.ThrowIfNull(key);
        Set(key.Path, value);
    }

    public bool TryGet(string path, out object? value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _values.TryGetValue(path, out value);
    }

    public bool TryGet<T>(UiStateKey<T> key, out T? value) {
        ArgumentNullException.ThrowIfNull(key);
        return TryGet(key.Path, out value);
    }

    public bool TryGet<T>(string path, out T? value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        value = default;
        if (!_values.TryGetValue(path, out var raw))
            return false;

        if (raw is T typed) {
            value = typed;
            return true;
        }

        return false;
    }

    public IDisposable Subscribe(string path, Action<object?> listener) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(listener);

        lock (_gate) {
            if (!_listeners.TryGetValue(path, out var registered)) {
                registered = [];
                _listeners[path] = registered;
            }

            registered.Add(listener);
        }

        return new Subscription(this, path, listener);
    }

    public IDisposable Subscribe<T>(UiStateKey<T> key, Action<T?> listener) {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(listener);

        return Subscribe(key.Path, value => {
            if (value is null) {
                listener(default);
                return;
            }

            if (value is T typed) {
                listener(typed);
                return;
            }

            listener(default);
        });
    }

    private void Unsubscribe(string path, Action<object?> listener) {
        lock (_gate) {
            if (!_listeners.TryGetValue(path, out var registered))
                return;

            registered.Remove(listener);
            if (registered.Count == 0)
                _listeners.Remove(path);
        }
    }

    private sealed class Subscription(UiStateStore owner, string path, Action<object?> listener) : IDisposable {
        private UiStateStore? _owner = owner;

        public void Dispose() {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.Unsubscribe(path, listener);
        }
    }
}
