// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Abstractions.UI.Actions;

public delegate void MarkupActionHandler(IMarkupActionContext context);

/// <summary>
/// Registers and executes named markup actions.
/// </summary>
public interface IMarkupActionRegistry {
    void Register(string name, MarkupActionHandler handler);
    bool TryExecute(IMarkupActionContext context);
}
