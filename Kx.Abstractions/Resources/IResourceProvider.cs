// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.Resources;

public interface IResourceProvider : IDisposable {
    string GetResourcePath(string relativePath);
    bool Exists(string relativePath);
}
