// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.DI;

using Microsoft.Extensions.DependencyInjection;

namespace Kx.Core.DI;

// Microsoft-DI Adapter
public class MsDiContainer : IDependencyContainer {
    private readonly IServiceCollection _services = new ServiceCollection();
    private IServiceProvider? _provider;

    // Container finalisieren
    public void Build() {
        _provider = _services.BuildServiceProvider();
    }

    // Service auflösen
    public T Get<T>() where T : class =>
        _provider!.GetRequiredService<T>();

    public object Get(Type type) =>
        _provider!.GetRequiredService(type);

    public IEnumerable<T> GetAll<T>() where T : class
        => _provider!.GetServices<T>();

    // Instanz mit DI + Parametern erzeugen
    public T Create<T>(params object[] args) where T : class =>
        ActivatorUtilities.CreateInstance<T>(_provider!, args);

    // --------------------------------------------------------
    // Register
    // --------------------------------------------------------
    public void Register<TService, TImpl>(Lifetime lifetime)
        where TService : class
        where TImpl : class, TService {
        switch (lifetime) {
            case Lifetime.Singleton:
                _services.AddSingleton<TService, TImpl>();
                break;

            case Lifetime.Transient:
                _services.AddTransient<TService, TImpl>();
                break;
        }
    }

    public void Register<TService>(TService instance)
        where TService : class =>
        _services.AddSingleton(instance);

    // --------------------------------------------------------
    // RegisterFactory
    // --------------------------------------------------------
    public void RegisterFactory<TService>(Lifetime lifetime, Func<IDependencyContainer, TService> factory)
        where TService : class {
        switch (lifetime) {
            case Lifetime.Singleton:
                _services.AddSingleton<TService>(_ => factory(this));
                break;

            case Lifetime.Transient:
                _services.AddTransient<TService>(_ => factory(this));
                break;
        }
    }

    // --------------------------------------------------------
    // AddSingleton / AddTransient (Microsoft-DI Style)
    // --------------------------------------------------------
    public void AddSingleton<TService, TImpl>()
        where TService : class
        where TImpl : class, TService =>
        _services.AddSingleton<TService, TImpl>();

    public void AddSingleton<TService>(TService instance)
        where TService : class =>
        _services.AddSingleton(instance);

    public void AddTransient<TService, TImpl>()
        where TService : class
        where TImpl : class, TService =>
        _services.AddTransient<TService, TImpl>();

    public void AddTransient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class =>
        _services.AddTransient(sp => factory(_provider!));

    // --------------------------------------------------------
    // TryAdd (überschreibt nicht)
    // --------------------------------------------------------
    public bool TryAddSingleton<TService, TImpl>()
        where TService : class
        where TImpl : class, TService {
        // Prüfen, ob Service bereits registriert ist
        bool exists = _services.Any(sd => sd.ServiceType == typeof(TService));
        if (exists)
            return false;

        _services.AddSingleton<TService, TImpl>();
        return true;
    }

    public bool TryAddTransient<TService, TImpl>()
        where TService : class
        where TImpl : class, TService {
        // Prüfen, ob Service bereits registriert ist
        bool exists = _services.Any(sd => sd.ServiceType == typeof(TService));
        if (exists)
            return false;

        _services.AddTransient<TService, TImpl>();
        return true;
    }
}
