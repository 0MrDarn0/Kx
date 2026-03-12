// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Abstractions.Events;
using Kx.Abstractions.UI.Actions;
using Kx.Abstractions.UI.VisualTree;

namespace Kx.UI.Actions;

internal static class BuiltInMarkupActionRegistrar {
    public static void Register(IMarkupActionRegistry registry) {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register("closeWindow", context => context.VisualContext.CloseWindow());
        registry.Register("toggleVisibility", context => ToggleVisibility(context));
        registry.Register("publishEvent", context => PublishEvent(context));
    }

    private static void ToggleVisibility(IMarkupActionContext context) {
        var targetId = string.IsNullOrWhiteSpace(context.Argument)
            ? context.Source.Id
            : context.Argument;

        if (!context.VisualContext.UIElementManager.TryGet(targetId, out var visual) || visual is null)
            throw new InvalidOperationException($"No visual with id '{targetId}' is registered for action '{context.ActionName}'.");

        visual.Visible = !visual.Visible;
    }

    private static void PublishEvent(IMarkupActionContext context) {
        if (string.IsNullOrWhiteSpace(context.Argument))
            throw new InvalidOperationException("The 'publishEvent' markup action requires an event name argument.");

        context.VisualContext.Events.NotifyAll(new MarkupActionEvent(context.Argument, context.Source.Id));
    }
}
