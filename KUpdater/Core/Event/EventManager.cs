// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using KUpdater.Scripting.Theme;
using MoonSharp.Interpreter;

namespace KUpdater.Core.Event;

/// <summary>
/// Ein einfacher, thread-sicherer EventManager.
/// Unterstützt sowohl synchrone (Action<T>) als auch asynchrone (AsyncAction<T>) Listener.
/// </summary>
public class EventManager : IEventManager {
    // Dictionary: Event-Typ -> Liste von Listener-Delegates
    private readonly ConcurrentDictionary<Type, List<Delegate>> _listeners = new();

    // Lock-Objekt für thread-sichere Zugriffe auf die Listener-Listen
    private readonly object _lock = new();

    private readonly ITheme? _theme;
    private readonly Dictionary<string, Type> _eventTypes = [];

    // Felder für dynamische Bindung
    private DynValue? _currentLuaFunc;

    public EventManager(ITheme? theme = null) {
        _theme = theme;

        // Mapping EventName -> Typ
        _eventTypes["StatusEvent"] = typeof(StatusEvent);
        _eventTypes["ProgressEvent"] = typeof(ProgressEvent);
        _eventTypes["UpdateRequired"] = typeof(UpdateRequired);
        _eventTypes["UpdatePipelineCompleted"] = typeof(UpdatePipelineCompleted);
        _eventTypes["ChangelogEvent"] = typeof(ChangelogEvent);
    }

    /// <summary>
    /// Registriert einen synchronen Listener für Nachrichten vom Typ T.
    /// </summary>
    public void Register<T>(Action<T> listener) {
        lock (_lock) {
            var list = _listeners.GetOrAdd(typeof(T), _ => []);
            list.Add(listener);
        }
    }

    public void Register(string eventName, DynValue luaFunc) {
        if (_theme == null)
            throw new InvalidOperationException("Lua runtime not set");

        if (!_eventTypes.TryGetValue(eventName, out var type))
            throw new ArgumentException($"Unknown event type {eventName}");

        // Delegate bauen: Action<T>
        var actionType = typeof(Action<>).MakeGenericType(type);

        // Lambda, das Lua aufruft
        var del = Delegate.CreateDelegate(
            actionType,
            typeof(EventManager).GetMethod(nameof(InvokeLuaForEvent), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
        );

        // Delegate dynamisch mit luaFunc und _lua binden
        var boundDel = Delegate.CreateDelegate(
            actionType,
            this,
            typeof(EventManager).GetMethod(nameof(InvokeLuaForEvent), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
        );

        // Aufruf
        var method = typeof(EventManager).GetMethod(nameof(Register), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(type);
        genericMethod.Invoke(this, new object[] { boundDel });
    }

    // Hilfsmethode für dynamischen Lua-Callback
    private void InvokeLuaForEvent(object ev) {
        if (_theme == null || _currentLuaFunc == null)
            throw new InvalidOperationException("Lua runtime or function not set");
        (_theme as MainTheme)?.SafeInvokeDyn(_currentLuaFunc, ev);
    }

    /// <summary>
    /// Entfernt einen synchronen Listener für Nachrichten vom Typ T.
    /// </summary>
    public void Unregister<T>(Action<T> listener) {
        lock (_lock) {
            if (_listeners.TryGetValue(typeof(T), out var list)) {
                list.Remove(listener);
                if (list.Count == 0)
                    _listeners.TryRemove(typeof(T), out _);
            }
        }
    }

    /// <summary>
    /// Benachrichtigt alle synchronen Listener für Nachrichten vom Typ T.
    /// </summary>
    public void NotifyAll<T>(T message) {
        if (_listeners.TryGetValue(typeof(T), out var listeners)) {
            Delegate[] snapshot;
            // Snapshot erstellen, um parallele Änderungen an der Liste zu vermeiden
            lock (_lock)
                snapshot = [.. listeners];

            foreach (var listener in snapshot.OfType<Action<T>>()) {
                try {
                    listener(message);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine($"Listener for {typeof(T).Name} threw: {ex}");
                }
            }
        }
    }

    /// <summary>
    /// Registriert einen asynchronen Listener für Nachrichten vom Typ T.
    /// </summary>
    public void RegisterAsync<T>(AsyncAction<T> listener) {
        lock (_lock) {
            var list = _listeners.GetOrAdd(typeof(T), _ => []);
            list.Add(listener);
        }
    }

    /// <summary>
    /// Entfernt einen asynchronen Listener für Nachrichten vom Typ T.
    /// </summary>
    public void UnregisterAsync<T>(AsyncAction<T> listener) {
        lock (_lock) {
            if (_listeners.TryGetValue(typeof(T), out var listeners)) {
                listeners.Remove(listener);
                if (listeners.Count == 0)
                    _listeners.TryRemove(typeof(T), out _);
            }
        }
    }

    /// <summary>
    /// Benachrichtigt alle Listener (sowohl sync als auch async) für Nachrichten vom Typ T.
    /// Synchrone Listener werden direkt ausgeführt,
    /// asynchrone Listener werden gesammelt und mit Task.WhenAll awaited.
    /// </summary>
    public async Task NotifyAllAsync<T>(T message) {
        if (_listeners.TryGetValue(typeof(T), out var listeners)) {
            Delegate[] snapshot;
            lock (_lock)
                snapshot = [.. listeners];

            var taskList = new List<Task>();
            foreach (var listener in snapshot) {
                switch (listener) {
                    case Action<T> syncListener:
                    try { syncListener(message); }
                    catch (Exception ex) {
                        Console.Error.WriteLine($"Listener for {typeof(T).Name} threw: {ex}");
                    }
                    break;

                    case AsyncAction<T> asyncListener:
                    try { taskList.Add(asyncListener(message)); }
                    catch (Exception ex) {
                        Console.Error.WriteLine($"AsyncListener for {typeof(T).Name} threw: {ex}");
                    }
                    break;
                }
            }

            if (taskList.Count > 0) {
                try { await Task.WhenAll(taskList); }
                catch (Exception ex) {
                    Console.Error.WriteLine($"One or more AsyncListeners for {typeof(T).Name} failed: {ex}");
                }
            }
        }
    }
}
