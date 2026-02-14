// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Event;

public interface IEventManager {
    // --- Sync / Async Listener Registrierung ---
    void Register<T>(Action<T> listener);
    void RegisterAsync<T>(AsyncAction<T> listener);
    void Unregister<T>(Action<T> listener);
    void UnregisterAsync<T>(AsyncAction<T> listener);

    // --- EventType Verwaltung ---
    bool RegisterEventType(string name, Type type);
    bool UnregisterEventType(string name);
    bool TryGetEventType(string name, out Type? type);
    IReadOnlyCollection<string> GetRegisteredEventNames();

    // --- Notify API ---
    void NotifyAll<T>(T message);
    Task NotifyAllAsync<T>(T message);
    void PrintAllEvents();
}
