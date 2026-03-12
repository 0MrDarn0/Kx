// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)


// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core;

namespace Kx.UI.Manager;

public static class UIContextProvider {
    public static WindowContext? Current { get; private set; }

    public static void Initialize(WindowContext ctx) => Current = ctx;
    public static void Clear() => Current = null;
}
