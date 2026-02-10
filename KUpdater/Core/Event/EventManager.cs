// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
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

        private ISkin? _skin;
        private readonly ConcurrentDictionary<Type, ImmutableArray<Delegate>> _listeners = new();
        private volatile ImmutableDictionary<string, Type> _eventTypes = ImmutableDictionary.Create<string, Type>(StringComparer.Ordinal);

        public EventManager(ISkin? skin = null) {
            _skin = skin;

            var builder = _eventTypes.ToBuilder();
            builder[nameof(StatusEvent)] = typeof(StatusEvent);
            builder[nameof(ProgressEvent)] = typeof(ProgressEvent);
            builder[nameof(UpdateRequired)] = typeof(UpdateRequired);
            builder[nameof(UpdatePipelineCompleted)] = typeof(UpdatePipelineCompleted);
            builder[nameof(ChangelogEvent)] = typeof(ChangelogEvent);
            builder[nameof(MainWindow_OnShown)] = typeof(MainWindow_OnShown);
            builder[nameof(MainWindow_OnFormClosed)] = typeof(MainWindow_OnFormClosed);
            _eventTypes = builder.ToImmutable();
        }

        public void SetSkin(ISkin skin) => _skin = skin;

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
            ArgumentNullException.ThrowIfNull(luaFunc);

            if (!TryGetEventType(eventName, out var type))
                throw new ArgumentException($"Unknown event type {eventName}", nameof(eventName));

            var method = typeof(EventManager).GetMethod(nameof(RegisterLuaGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var generic = method.MakeGenericMethod(type!);
            generic.Invoke(this, [luaFunc]);
        }

        public bool TryRegisterLua(string eventName, DynValue luaFunc) {
            if (luaFunc == null)
                return false;

            if (!TryGetEventType(eventName, out var type) || type == null)
                return false;

            try {
                var method = typeof(EventManager).GetMethod(nameof(RegisterLuaGeneric), BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    return false;

                var generic = method.MakeGenericMethod(type);
                generic.Invoke(this, [luaFunc]);

                return true;
            }
            catch {
                // Fehler in Lua oder Reflection → false zurückgeben
                return false;
            }
        }

        // Generic helper, erzeugt eine Closure die die DynValue referenziert
        private void RegisterLuaGeneric<T>(DynValue luaFunc) {
            if (_skin == null)
                throw new InvalidOperationException("Lua runtime not set");

            void action(T ev) {
                try {
                    ((SkinBase)_skin)?.SafeInvokeDyn(luaFunc, ev!);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine($"Lua listener for {typeof(T).Name} threw: {ex}");
                }
            }

            Register((Action<T>)action);
        }

        #endregion

        #region EventType Regestrierung

        private static void ValidateEventType(string name, Type type) {
            ArgumentNullException.ThrowIfNull(type);

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Event name must not be empty.", nameof(name));

            if (!type.IsClass)
                throw new ArgumentException("Event type must be a class.", nameof(type));

            if (type.IsAbstract)
                throw new ArgumentException("Event type must not be abstract.", nameof(type));

            if (!typeof(IEvent).IsAssignableFrom(type))
                throw new ArgumentException("Event type must implement IEvent.", nameof(type));
        }

        // Atomare Registrierung: gibt true zurück wenn hinzugefügt oder überschrieben
        public bool RegisterEventType(string name, Type type) {
            ValidateEventType(name, type);
            while (true) {
                var snapshot = _eventTypes;

                var builder = snapshot.ToBuilder();
                builder[name] = type;

                var updated = builder.ToImmutable();
                var original = Interlocked.CompareExchange(ref _eventTypes, updated, snapshot);
                if (ReferenceEquals(original, snapshot)) {
                    return true;
                }
                // sonst: jemand hat zwischenzeitlich geschrieben -> retry
            }
        }

        // Entfernen: true wenn Key existierte und entfernt wurde
        public bool UnregisterEventType(string name) {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Event name must not be empty.", nameof(name));

            while (true) {
                var snapshot = _eventTypes;

                if (!snapshot.ContainsKey(name))
                    return false;

                var builder = snapshot.ToBuilder();
                builder.Remove(name);
                var updated = builder.ToImmutable();
                var original = Interlocked.CompareExchange(ref _eventTypes, updated, snapshot);

                if (ReferenceEquals(original, snapshot)) {
                    return true;
                }
                // retry
            }
        }

        public bool TryGetEventType(string name, out Type? type) {
            if (string.IsNullOrWhiteSpace(name)) {
                type = null;
                return false;
            }
            return _eventTypes.TryGetValue(name, out type);
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

        #region Hilfsmethoden für Event Management

        public IReadOnlyCollection<string> GetRegisteredEventNames() => [.. _eventTypes.Keys];

        #endregion

        #region Hilfsmethoden für Listener Management

        private void AddListener(Type eventType, Delegate listener) {
            _listeners.AddOrUpdate(
                eventType,
                [listener],
                (_, old) => old.Add(listener)
            );
        }

        private void RemoveListener(Type eventType, Delegate listener) {
            // Versuche in einer Schleife, da TryUpdate fehlschlagen kann wenn zwischenzeitlich ein anderer Thread schreibt
            _listeners.AddOrUpdate(
                eventType,
                [],
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

        public void PrintAllEvents() {
            Debug.WriteLine("=== Registered Event Types ===");

            foreach (var kv in _eventTypes) {
                var name = kv.Key;
                var type = kv.Value;

                // Listener count ermitteln
                int listenerCount = 0;
                if (_listeners.TryGetValue(type, out var arr) && !arr.IsDefaultOrEmpty)
                    listenerCount = arr.Length;

                Debug.WriteLine($"- {name}  →  {type.FullName}  (Listeners: {listenerCount})");
            }

            Debug.WriteLine("=== End of Event List ===");
        }

    }
}
