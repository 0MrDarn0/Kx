// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Config;

public interface IConfigLoader {
    T Load<T>(string path) where T : class, new();
}
