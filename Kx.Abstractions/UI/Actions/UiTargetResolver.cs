// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.UI.Elements;
using Kx.Abstractions.UI.VisualTree;

namespace Kx.Abstractions.UI.Actions;

/// <summary>
/// Resolves markup target expressions against the current UI tree.
/// </summary>
public static class UiTargetResolver {
    public static bool TryResolve(UIElement source, string? expression, out IVisual? visual) {
        ArgumentNullException.ThrowIfNull(source);

        if (!UiTargetReference.TryParse(expression, out var target) || target is null) {
            visual = null;
            return false;
        }

        switch (target.Kind) {
            case UiTargetKind.Self:
                visual = source;
                return true;

            case UiTargetKind.Parent:
                visual = source.Parent;
                return visual is not null;

            case UiTargetKind.Root:
                visual = ResolveRoot(source);
                return visual is not null;

            case UiTargetKind.Id:
                if (string.IsNullOrWhiteSpace(target.Value)) {
                    visual = null;
                    return false;
                }

                return source.Context.UIElementManager.TryGet(target.Value, out visual);

            default:
                visual = null;
                return false;
        }
    }

    private static UIElement ResolveRoot(UIElement source) {
        var current = source;

        while (current.Parent is not null)
            current = current.Parent;

        return current;
    }
}
