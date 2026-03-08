// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.DI;

// ------------------------------------------------------------
// Auflösung von Services
// ------------------------------------------------------------
public interface IServiceResolver {
    T Get<T>() where T : class;
    object Get(Type type);
}
