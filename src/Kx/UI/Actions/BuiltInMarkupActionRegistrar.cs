// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI.Actions;
using Kx.Sdk.UI.Commands;
using Kx.Sdk.UI.Payloads;
using Kx.Sdk.UI.VisualTree;

namespace Kx.UI.Actions;

internal static class BuiltInMarkupActionRegistrar {
    public static void Register(IMarkupActionRegistry registry) {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register("closeWindow", context => context.VisualContext.CloseWindow());
        registry.Register("openWindow", context => OpenWindow(context));
        registry.Register("enable", context => SetEnabled(context, true));
        registry.Register("disable", context => SetEnabled(context, false));
        registry.Register("focus", context => Focus(context));
        registry.Register("show", context => SetVisibility(context, true));
        registry.Register("hide", context => SetVisibility(context, false));
        registry.Register("setColor", context => SetColor(context));
        registry.Register("toggleVisibility", context => ToggleVisibility(context));
        registry.Register("setText", context => SetText(context));
        registry.Register("publishEvent", context => PublishEvent(context));
        registry.Register("runCommand", context => RunCommand(context));
    }

    private static void OpenWindow(IMarkupActionContext context) {
        if (string.IsNullOrWhiteSpace(context.Argument))
            throw new InvalidOperationException("The 'openWindow' markup action requires a window definition name argument.");

        if (TryDeserialize<OpenWindowPayload>(context.Argument, out var payload) && payload is not null) {
            context.VisualContext.OpenWindow(payload.WindowName);
            return;
        }

        context.VisualContext.OpenWindow(context.Argument);
    }

    private static void ToggleVisibility(IMarkupActionContext context) {
        var visual = ResolveTarget(context, context.Argument);

        visual.Visible = !visual.Visible;
    }

    private static void SetEnabled(IMarkupActionContext context, bool enabled) {
        string? targetExpression = context.Argument;
        if (TryDeserialize<EnabledStatePayload>(context.Argument, out var payload) && payload is not null) {
            targetExpression = payload.TargetId;
            enabled = payload.Enabled;
        }

        var visual = ResolveTarget(context, targetExpression);
        if (visual is not Kx.UI.Elements.Button button)
            throw new InvalidOperationException($"The '{context.ActionName}' markup action does not support visuals of type '{visual.GetType().Name}'.");

        button.IsEnabled = enabled;
        button.Context.RequestRender();
    }

    private static void Focus(IMarkupActionContext context) {
        var visual = ResolveTarget(context, context.Argument);
        if (!visual.CanFocus)
            throw new InvalidOperationException($"The 'focus' markup action does not support visuals of type '{visual.GetType().Name}'.");

        context.VisualContext.UIElementManager.SetFocus(visual);
        context.VisualContext.RequestRender();
    }

    private static void SetVisibility(IMarkupActionContext context, bool visible) {
        string? targetExpression = context.Argument;
        if (TryDeserialize<VisibilityPayload>(context.Argument, out var payload) && payload is not null) {
            targetExpression = payload.TargetId;
            visible = payload.Visible;
        }

        var visual = ResolveTarget(context, targetExpression);

        visual.Visible = visible;
    }

    private static void SetText(IMarkupActionContext context) {
        string targetId;
        string value;

        if (TryDeserialize<TextUpdatePayload>(context.Argument, out var payload) && payload is not null) {
            targetId = payload.TargetId;
            value = payload.Text;
        } else if (!TryParseTargetAndValue(context.Argument, out targetId, out value)) {
            throw new InvalidOperationException("The 'setText' markup action requires an argument in the format 'targetId|value'.");
        }

        var visual = ResolveTarget(context, targetId);

        switch (visual) {
            case Kx.UI.Elements.Label label:
                label.Text.Value = value;
                break;

            case Kx.UI.Elements.Button button:
                button.Text = value;
                button.Context.RequestRender();
                break;

            default:
                throw new InvalidOperationException($"The 'setText' markup action does not support visuals of type '{visual.GetType().Name}'.");
        }
    }

    private static void SetColor(IMarkupActionContext context) {
        string targetId;
        string colorValue;

        if (TryDeserialize<ColorUpdatePayload>(context.Argument, out var payload) && payload is not null) {
            targetId = payload.TargetId;
            colorValue = payload.Color;
        } else if (!TryParseTargetAndValue(context.Argument, out targetId, out colorValue)) {
            throw new InvalidOperationException("The 'setColor' markup action requires an argument in the format 'targetId|color'.");
        }

        var visual = ResolveTarget(context, targetId);
        var color = KxColor.Parse(colorValue);

        switch (visual) {
            case Kx.UI.Elements.Label label:
                label.ForegroundColor = color;
                break;

            default:
                throw new InvalidOperationException($"The 'setColor' markup action does not support visuals of type '{visual.GetType().Name}'.");
        }
    }

    private static void PublishEvent(IMarkupActionContext context) {
        if (string.IsNullOrWhiteSpace(context.Argument))
            throw new InvalidOperationException("The 'publishEvent' markup action requires an event name argument.");

        if (TryDeserialize<EventPublishPayload>(context.Argument, out var eventPayload) && eventPayload is not null) {
            context.VisualContext.Events.NotifyAll(new MarkupActionEvent(eventPayload.EventName, context.Source.Id, eventPayload.PayloadJson));
            return;
        }

        if (!TryParseTargetAndValue(context.Argument, out var eventName, out var payload)) {
            eventName = context.Argument;
            payload = null;
        }

        context.VisualContext.Events.NotifyAll(new MarkupActionEvent(eventName, context.Source.Id, payload));
    }

    private static void RunCommand(IMarkupActionContext context) {
        if (!TryParseTargetAndValue(context.Argument, out var commandName, out var argument)) {
            commandName = context.Argument?.Trim() ?? string.Empty;
            argument = null;
        }

        if (string.IsNullOrWhiteSpace(commandName))
            throw new InvalidOperationException("The 'runCommand' markup action requires a command name argument.");

        var commandContext = new Kx.UI.Commands.UiCommandContext(context.VisualContext, context.Source, commandName, argument);
        if (!context.VisualContext.Commands.TryExecute(commandContext))
            throw new InvalidOperationException($"No UI command has been registered for '{commandName}'.");
    }

    private static IVisual ResolveTarget(IMarkupActionContext context, string? expression) {
        if (!UiTargetResolver.TryResolve(context.Source, expression, out var visual) || visual is null)
            throw new InvalidOperationException($"No visual matching target '{expression ?? "self"}' is registered for action '{context.ActionName}'.");

        return visual;
    }

    private static bool TryParseTargetAndValue(string? argument, out string targetId, out string value) {
        targetId = string.Empty;
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(argument))
            return false;

        int separatorIndex = argument.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == argument.Length - 1)
            return false;

        targetId = argument[..separatorIndex].Trim();
        value = argument[(separatorIndex + 1)..];
        return !string.IsNullOrWhiteSpace(targetId);
    }

    private static bool TryDeserialize<T>(string? raw, out T? value) {
        var payload = new UiCommandPayload(raw);
        return payload.TryDeserialize(out value);
    }
}
