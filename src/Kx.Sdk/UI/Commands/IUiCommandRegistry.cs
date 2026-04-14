// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Commands;

public delegate void UiCommandHandler(IUiCommandContext context);

/// <summary>
/// Registers and executes named UI commands.
/// </summary>
public interface IUiCommandRegistry {
    void Register(string name, UiCommandHandler handler);
    bool TryExecute(IUiCommandContext context);
}
