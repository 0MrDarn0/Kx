// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Events;

public interface IEventManager {
    // --- Sync / Async Listener Registrierung ---
    void Register<T>(Action<T> listener) where T : IEvent;
    void RegisterAsync<T>(AsyncAction<T> listener) where T : IEvent;
    void Unregister<T>(Action<T> listener) where T : IEvent;
    void UnregisterAsync<T>(AsyncAction<T> listener) where T : IEvent;

    // --- EventType Verwaltung ---
    bool RegisterEventType(string name, Type type);
    bool UnregisterEventType(string name);
    bool TryGetEventType(string name, out Type? type);
    IReadOnlyCollection<string> GetRegisteredEventNames();

    // --- Notify API ---
    void NotifyAll<T>(T message) where T : IEvent;
    Task NotifyAllAsync<T>(T message) where T : IEvent;
    void PrintAllEvents();
}
