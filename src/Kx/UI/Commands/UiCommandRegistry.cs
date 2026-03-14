// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.UI.Commands;

namespace Kx.UI.Commands;

public sealed class UiCommandRegistry : IUiCommandRegistry {
    private readonly Dictionary<string, UiCommandHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, UiCommandHandler handler) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);

        _handlers[name] = handler;
    }

    public bool TryExecute(IUiCommandContext context) {
        ArgumentNullException.ThrowIfNull(context);

        if (!_handlers.TryGetValue(context.CommandName, out var handler))
            return false;

        handler(context);
        return true;
    }
}
