// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.DI;

// ------------------------------------------------------------
// Dependency Container
// ------------------------------------------------------------
public interface IDependencyContainer : IServiceRegistry, IServiceResolver {
    // Container final bauen
    void Build();

    // Instanz mit DI + zusätzlichen Parametern erzeugen
    T Create<T>(params object[] args) where T : class;
}
