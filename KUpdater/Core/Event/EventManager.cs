// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using KUpdater.Scripting.Skin;
using MoonSharp.Interpreter;

namespace KUpdater.Core.Event {
    /// <summary>
    /// Thread‑sicherer EventManager.
    /// Unterstützt synchrone Action<T> und asynchrone AsyncAction<T> Listener.
    /// Bietet außerdem Registrierung von Lua‑Funktionen per DynValue.
    /// </summary>
    public class EventManager : IEventManager {
        // Mapping: EventType -> unveränderliche Liste von Delegates
        private readonly ConcurrentDictionary<Type, ImmutableArray<Delegate>> _listeners
            = new();

        private readonly ISkin? _skin;

        // Mapping EventName -> Type für Lua-Registrierung
        private readonly Dictionary<string, Type> _eventTypes = new(StringComparer.Ordinal);

        public EventManager(ISkin? skin = null) {
            _skin = skin;

            // Vorbelegte Eventtypen (wie im Original)
            _eventTypes["StatusEvent"] = typeof(StatusEvent);
            _eventTypes["ProgressEvent"] = typeof(ProgressEvent);
            _eventTypes["UpdateRequired"] = typeof(UpdateRequired);
            _eventTypes["UpdatePipelineCompleted"] = typeof(UpdatePipelineCompleted);
            _eventTypes["ChangelogEvent"] = typeof(ChangelogEvent);
        }

        #region Registrierung Sync / Async

        public void Register<T>(Action<T> listener) {
            AddListener(typeof(T), listener);
        }

        public void RegisterAsync<T>(AsyncAction<T> listener) {
            AddListener(typeof(T), listener);
        }

        public void Unregister<T>(Action<T> listener) {
            RemoveListener(typeof(T), listener);
        }

        public void UnregisterAsync<T>(AsyncAction<T> listener) {
            RemoveListener(typeof(T), listener);
        }

        #endregion

        #region Lua Registrierung

        /// <summary>
        /// Registriert eine Lua Funktion (DynValue) für ein Event per Eventname.
        /// Jede Registrierung erzeugt eine eigene Closure, damit mehrere Lua-Handler koexistieren.
        /// </summary>
        public void Register(string eventName, DynValue luaFunc) {
            if (luaFunc == null)
                throw new ArgumentNullException(nameof(luaFunc));
            if (!_eventTypes.TryGetValue(eventName, out var type))
                throw new ArgumentException($"Unknown event type {eventName}", nameof(eventName));

            // Invoke generic helper RegisterLuaGeneric<T>(DynValue)
            var method = typeof(EventManager).GetMethod(nameof(RegisterLuaGeneric),
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var generic = method.MakeGenericMethod(type);
            generic.Invoke(this, new object[] { luaFunc });
        }

        // Generic helper, erzeugt eine Closure die die DynValue referenziert
        private void RegisterLuaGeneric<T>(DynValue luaFunc) {
            if (_skin == null)
                throw new InvalidOperationException("Lua runtime not set");

            Action<T> action = ev =>
            {
                try
                {
                    // SafeInvokeDyn ist Teil SkinBase; falls _skin nicht SkinBase ist, cast prüfen
                    (_skin as SkinBase)?.SafeInvokeDyn(luaFunc, ev);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Lua listener for {typeof(T).Name} threw: {ex}");
                }
            };

            Register(action);
        }

        #endregion

        #region Notify Methoden

        /// <summary>
        /// Benachrichtigt alle synchronen Listener für Nachrichten vom Typ T.
        /// </summary>
        public void NotifyAll<T>(T message) {
            if (_listeners.TryGetValue(typeof(T), out var arr) && !arr.IsDefaultOrEmpty) {
                // Snapshot ist arr selbst (ImmutableArray ist threadsicher)
                foreach (var del in arr) {
                    if (del is Action<T> sync) {
                        try {
                            sync(message);
                        }
                        catch (Exception ex) {
                            Console.Error.WriteLine($"Listener for {typeof(T).Name} threw: {ex}");
                        }
                    }
                    // Falls ein AsyncAction fälschlich hier liegt, ignoriere es (NotifyAllAsync behandelt async)
                }
            }
        }

        /// <summary>
        /// Benachrichtigt alle Listener (sync + async). Async Listener werden gesammelt und awaited.
        /// </summary>
        public async Task NotifyAllAsync<T>(T message) {
            if (_listeners.TryGetValue(typeof(T), out var arr) && !arr.IsDefaultOrEmpty) {
                var tasks = new List<Task>(capacity: 4);

                foreach (var del in arr) {
                    switch (del) {
                        case Action<T> sync:
                        try {
                            sync(message);
                        }
                        catch (Exception ex) {
                            Console.Error.WriteLine($"Listener for {typeof(T).Name} threw: {ex}");
                        }
                        break;

                        case AsyncAction<T> async:
                        try {
                            var t = async(message);
                            if (t != null)
                                tasks.Add(t);
                        }
                        catch (Exception ex) {
                            Console.Error.WriteLine($"AsyncListener for {typeof(T).Name} threw: {ex}");
                        }
                        break;

                        default:
                        // Unbekannter Delegate‑Typ: ignoriere, aber logge
                        Console.Error.WriteLine($"Unknown listener delegate type for {typeof(T).Name}: {del.GetType().FullName}");
                        break;
                    }
                }

                if (tasks.Count > 0) {
                    try {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch (Exception ex) {
                        Console.Error.WriteLine($"One or more AsyncListeners for {typeof(T).Name} failed: {ex}");
                    }
                }
            }
        }

        #endregion

        #region Hilfsmethoden für Listener Management

        private void AddListener(Type eventType, Delegate listener) {
            _listeners.AddOrUpdate(
                eventType,
                ImmutableArray.Create(listener),
                (_, old) => old.Add(listener)
            );
        }

        private void RemoveListener(Type eventType, Delegate listener) {
            // Versuche in einer Schleife, da TryUpdate fehlschlagen kann wenn zwischenzeitlich ein anderer Thread schreibt
            _listeners.AddOrUpdate(
                eventType,
                ImmutableArray<Delegate>.Empty,
                (type, old) => {
                    var updated = old.Remove(listener);
                    return updated;
                });

            // Optional: Wenn Liste leer ist, entferne den Key
            if (_listeners.TryGetValue(eventType, out var current) && current.IsDefaultOrEmpty) {
                _listeners.TryRemove(eventType, out _);
            }
        }

        #endregion
    }
}
