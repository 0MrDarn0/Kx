# Plugin UI Markup Examples

## Goal

This document shows practical examples for the current plugin-driven UI framework.

It focuses on:

- themes
- window definitions
- merged overrides
- nested controls
- built-in actions
- custom actions
- commands
- typed payloads
- target resolution

## 1. Theme registration

A theme provides reusable defaults for:

- frame styling
- default controls

Example:

```csharp
themeRegistry.Register("Example.Dark", new WindowTheme {
    Frame = new FrameConfig {
        Style = FrameStyle.Default,
        Default = new DefaultFrameConfig {
            Title = "Example Plugin Window",
            BackgroundColor = "#1B1D22",
            TitleBarColor = "#262A33",
            BorderColor = "#4C7FFF",
            SeparatorColor = "#394052"
        }
    },
    Controls = [
        new ControlConfig {
            Type = "StackPanel",
            Id = "example_panel",
            Layer = "Content",
            Bounds = new BoundsConfig {
                X = 24,
                Y = 24,
                Width = 220,
                Height = 360
            },
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["orientation"] = "Vertical",
                ["spacing"] = "8"
            }
        }
    ]
});
```

## 2. Window definition registration

A window definition can:

- reference a theme
- override parts of the frame
- override controls by `Id`
- add additional controls

Example:

```csharp
windowRegistry.Register("MainWindow", new WindowConfig {
    Theme = "Example.Dark",
    Frame = new FrameConfig {
        Default = new DefaultFrameConfig {
            Title = "Merged Main Window",
            BorderColor = "#FF8C42"
        }
    },
    Controls = [
        new ControlConfig {
            Id = "example_panel",
            Children = [
                new ControlConfig {
                    Id = "example_title",
                    Text = "Plugin Window (merged)",
                    Color = "#FFD166"
                },
                new ControlConfig {
                    Type = "Label",
                    Id = "example_merge_hint",
                    Text = "Added by WindowConfig merge",
                    Color = "#8BD3FF"
                }
            ]
        }
    ]
});
```

## 3. Merge behavior

Current merge rules are:

### Frame

- theme frame is the base
- window frame overrides theme fields
- override detection currently depends on config default values

### Controls

- theme controls are the base list
- window controls are matched by `Id`
- matching controls are merged
- unmatched window controls are appended
- nested `Children` are merged recursively by `Id`
- `Properties` are overridden key by key

## 4. Nested control trees

A `ControlConfig` can contain nested `Children`.

Example:

```csharp
new ControlConfig {
    Type = "StackPanel",
    Id = "example_panel",
    Layer = "Content",
    Padding = new ThicknessConfig {
        Left = 12,
        Top = 12,
        Right = 12,
        Bottom = 12
    },
    Bounds = new BoundsConfig {
        X = 24,
        Y = 24,
        Width = 220,
        Height = 360
    },
    Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["orientation"] = "Vertical",
        ["spacing"] = "8"
    },
    Children = [
        new ControlConfig {
            Type = "Label",
            Id = "example_title",
            Text = "Plugin Window"
        },
        new ControlConfig {
            Type = "ExampleBadge",
            Id = "example_badge",
            Text = "Plugin Content"
        },
        new ControlConfig {
            Type = "Button",
            Id = "example_toggle_button",
            Text = "Toggle Badge",
            OnClick = "example.toggleVisibility:id:example_badge"
        }
    ]
}
```

## 5. Built-in actions

Current built-in actions include:

- `closeWindow`
- `openWindow`
- `enable`
- `disable`
- `focus`
- `show`
- `hide`
- `setColor`
- `setText`
- `toggleVisibility`
- `publishEvent`
- `runCommand`

## 6. Target syntax

Supported target expressions:

- `self`
- `parent`
- `root`
- `id:example_badge`
- `example_badge` as legacy fallback

Examples:

```csharp
OnClick = "hide:self"
OnClick = "show:parent"
OnClick = "toggleVisibility:id:example_badge"
OnClick = "focus:id:example_toggle_button"
```

## 7. Legacy string action arguments

Some actions still support the older string-based forms.

Examples:

```csharp
OnClick = "setText:id:example_title|Updated by markup"
OnClick = "openWindow:Example.Alternate"
OnClick = "publishEvent:SomethingHappened"
```

## 8. Shared payload schemas

Common payload DTOs live in `Kx.Abstractions.UI.Payloads`.

Current shared payloads:

- `TargetPayload`
- `TextUpdatePayload`
- `VisibilityPayload`
- `OpenWindowPayload`
- `EnabledStatePayload`
- `ColorUpdatePayload`
- `EventPublishPayload`

These can be used in JSON payloads for built-in actions, commands, and events.

## 9. JSON payload examples for built-in actions

### Show / hide

```csharp
OnClick = "show:{\"targetId\":\"id:example_badge\",\"visible\":true}"
OnClick = "hide:{\"targetId\":\"id:example_badge\",\"visible\":false}"
```

### Enable / disable

```csharp
OnClick = "enable:id:example_toggle_button"
OnClick = "disable:{\"targetId\":\"id:example_toggle_button\",\"enabled\":false}"
```

### Set text

```csharp
OnClick = "setText:{\"targetId\":\"id:example_title\",\"text\":\"Updated by payload\"}"
```

### Set color

```csharp
OnClick = "setColor:{\"targetId\":\"id:example_title\",\"color\":\"#FFD166\"}"
```

### Open window

```csharp
OnClick = "openWindow:{\"windowName\":\"Example.Alternate\"}"
```

## 10. Custom action example

A plugin can register its own action:

```csharp
actionRegistry.Register("example.toggleVisibility", actionContext => ToggleVisibility(actionContext));
```

And consume it from markup:

```csharp
OnClick = "example.toggleVisibility:id:example_badge"
```

Example implementation:

```csharp
private static void ToggleVisibility(IMarkupActionContext actionContext) {
    if (!UiTargetResolver.TryResolve(actionContext.Source, actionContext.Argument, out var visual) || visual is null)
        return;

    visual.Visible = !visual.Visible;
}
```

## 11. Custom command example

A plugin can register a command:

```csharp
commandRegistry.Register("example.renameBadge", commandContext => RenameBadge(commandContext));
```

And invoke it from markup:

```csharp
OnClick = "runCommand:example.renameBadge|{\"targetId\":\"example_badge\",\"text\":\"Updated by command\"}"
```

Example implementation:

```csharp
private static void RenameBadge(IUiCommandContext commandContext) {
    if (!commandContext.Payload.TryDeserialize<TextUpdatePayload>(out var payload) || payload is null)
        return;

    if (!commandContext.VisualContext.UIElementManager.TryGet(payload.TargetId, out var visual) || visual is not ExampleBadge badge)
        return;

    badge.SetText(payload.Text);
}
```

## 12. Typed event payload example

Markup can publish typed event payloads:

```csharp
OnClick = "publishEvent:BadgeUpdated|{\"targetId\":\"example_badge\",\"text\":\"Updated by event\"}"
```

Consumer example:

```csharp
context.VisualContext.Events.Register<MarkupActionEvent>(e => {
    if (e.EventName == "BadgeUpdated" && e.Payload.TryDeserialize<TextUpdatePayload>(out var payload)) {
        // use payload.TargetId and payload.Text
    }
});
```

## 13. Alternate window example

A second registered window definition can be opened via action:

```csharp
windowRegistry.Register("Example.Alternate", new WindowConfig {
    Theme = "Example.Alternate"
});
```

Markup:

```csharp
OnClick = "openWindow:Example.Alternate"
```

Back-navigation:

```csharp
OnClick = "openWindow:MainWindow"
```

## 14. Current practical recommendations

### Prefer themes for defaults
Put reusable base styling and reusable base controls into:

- `WindowTheme`

### Prefer window definitions for scenario-specific overrides
Put concrete per-window adjustments into:

- `WindowConfig`

### Prefer shared payload DTOs
Prefer:

- `TextUpdatePayload`
- `VisibilityPayload`
- `OpenWindowPayload`
- `EnabledStatePayload`
- `ColorUpdatePayload`
- `EventPublishPayload`

instead of plugin-local mini payload records where possible.

### Prefer custom commands for behavior
If the behavior is application- or plugin-specific, prefer:

- `runCommand:...`

instead of endlessly growing the built-in action list.

## 15. Current limitations

Still worth keeping in mind:

- action argument syntax is still string-based at the outermost level
- merge override detection for frame values still depends on config defaults
- targeting is improved, but still intentionally simple
- no full binding/viewmodel layer for markup-defined controls yet
