// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Abstractions.UI;

public interface IUiContext {
    object Backend { get; }
    object Events { get; }
    object Controls { get; }
}
