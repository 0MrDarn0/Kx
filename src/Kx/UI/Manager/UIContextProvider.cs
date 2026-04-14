// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)


// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI;

namespace Kx.UI.Manager;

public static class UIContextProvider {
    public static IVisualContext? Current { get; private set; }

    public static void Initialize(IVisualContext ctx) => Current = ctx;
    public static void Clear() => Current = null;
}
