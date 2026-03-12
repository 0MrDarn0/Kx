// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.Lifecycle;

public interface IShutdownAware {
    ValueTask ShutdownAsync();
}
