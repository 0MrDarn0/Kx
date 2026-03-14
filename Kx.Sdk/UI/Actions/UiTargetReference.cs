// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.UI.Actions;

/// <summary>
/// Represents a parsed markup target reference.
/// </summary>
public sealed record UiTargetReference(UiTargetKind Kind, string? Value = null) {
    public static bool TryParse(string? expression, out UiTargetReference? target) {
        target = null;

        if (string.IsNullOrWhiteSpace(expression) || string.Equals(expression, "self", StringComparison.OrdinalIgnoreCase)) {
            target = new UiTargetReference(UiTargetKind.Self);
            return true;
        }

        if (string.Equals(expression, "parent", StringComparison.OrdinalIgnoreCase)) {
            target = new UiTargetReference(UiTargetKind.Parent);
            return true;
        }

        if (string.Equals(expression, "root", StringComparison.OrdinalIgnoreCase)) {
            target = new UiTargetReference(UiTargetKind.Root);
            return true;
        }

        const string idPrefix = "id:";
        if (expression.StartsWith(idPrefix, StringComparison.OrdinalIgnoreCase)) {
            var id = expression[idPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(id))
                return false;

            target = new UiTargetReference(UiTargetKind.Id, id);
            return true;
        }

        target = new UiTargetReference(UiTargetKind.Id, expression);
        return true;
    }
}
