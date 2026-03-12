// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI.Actions;

namespace Kx.UI.Actions;

public sealed class MarkupActionRegistry : IMarkupActionRegistry {
    private readonly Dictionary<string, MarkupActionHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, MarkupActionHandler handler) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);

        _handlers[name] = handler;
    }

    public bool TryExecute(IMarkupActionContext context) {
        ArgumentNullException.ThrowIfNull(context);

        if (!_handlers.TryGetValue(context.ActionName, out var handler))
            return false;

        handler(context);
        return true;
    }
}
