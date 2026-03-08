// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.DI;

// Registrierung von Services
public interface IServiceRegistry {

    // Einheitliche Registrierung über Lifetime
    void Register<TService, TImpl>(Lifetime lifetime)
        where TService : class
        where TImpl : class, TService;

    // Singleton-Instanz direkt registrieren
    void Register<TService>(TService instance)
        where TService : class;

    // Factory registrieren
    void RegisterFactory<TService>(Lifetime lifetime, Func<IDependencyContainer, TService> factory)
    where TService : class;

    // Microsoft-DI kompatible Methoden
    void AddSingleton<TService, TImpl>()
        where TService : class
        where TImpl : class, TService;

    void AddSingleton<TService>(TService instance)
        where TService : class;

    void AddTransient<TService, TImpl>()
        where TService : class
        where TImpl : class, TService;

    void AddTransient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class;

    // Sichere Registrierung (überschreibt nicht)
    bool TryAddSingleton<TService, TImpl>()
        where TService : class
        where TImpl : class, TService;

    bool TryAddTransient<TService, TImpl>()
        where TService : class
        where TImpl : class, TService;
}
