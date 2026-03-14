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
- state bindings
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

## 8. State seeding

Plugins can seed initial UI state through `IUiStateStore`.

Preferred code-side usage is now via typed keys:

Example:

```csharp
var stateStore = context.Services.Get<IUiStateStore>();

UiStateKey<string> titleState = new("example.title");
UiStateKey<string> titleColorState = new("example.titleColor");
UiStateKey<float> titleFontSizeState = new("example.titleFontSize");
UiStateKey<string> badgeTextState = new("example.badgeText");
UiStateKey<bool> mergeHintVisibleState = new("example.mergeHintVisible");
UiStateKey<float> panelSpacingState = new("example.panelSpacing");
UiStateKey<string> panelOrientationState = new("example.panelOrientation");
UiStateKey<float> buttonFontSizeState = new("example.buttonFontSize");

stateStore.Set(titleState, "Plugin Window (bound)");
stateStore.Set(titleColorState, "#F5F5F5");
stateStore.Set(titleFontSizeState, 16f);
stateStore.Set(badgeTextState, "Plugin Content");
stateStore.Set(mergeHintVisibleState, true);
stateStore.Set(panelSpacingState, 8f);
stateStore.Set(panelOrientationState, "Vertical");
stateStore.Set(buttonFontSizeState, 14f);
```

## 9. Markup bindings

Current binding-capable `ControlConfig` fields:

- `TextBinding`
- `ColorBinding`
- `VisibleBinding`
- `EnabledBinding`
- `FontSizeBinding`
- `OrientationBinding`
- `SpacingBinding`

Example:

```csharp
new ControlConfig {
    Type = "Label",
    Id = "example_title",
    Text = "Plugin Window",
    TextBinding = titleState.Path,
    Color = "#F5F5F5",
    ColorBinding = titleColorState.Path,
    FontSizeBinding = titleFontSizeState.Path
}
```

Current built-in bindings support:

- `Label.Text`
- `Label.Color`
- `Label.Font` size
- `UIElement.Visible`
- `Button.Text`
- `Button.IsEnabled`
- `Button.FontSize`
- `StackPanel.Orientation`
- `StackPanel.Spacing`

Additional example:

```csharp
new ControlConfig {
    Type = "StackPanel",
    Id = "example_panel",
    OrientationBinding = panelOrientationState.Path,
    SpacingBinding = panelSpacingState.Path,
    Children = [
        new ControlConfig {
            Type = "Button",
            Id = "example_toggle_button",
            Text = "Toggle Badge",
            FontSizeBinding = buttonFontSizeState.Path,
            OnClick = "example.toggleVisibility:id:example_badge"
        }
    ]
}
```

## 10. Shared payload schemas

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

## 11. JSON payload examples for built-in actions

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

## 12. Custom action example

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

## 13. Custom command example

A plugin can register a command:

```csharp
commandRegistry.Register("example.renameBadge", commandContext => RenameBadge(stateStore, commandContext));
```

And invoke it from markup:

```csharp
OnClick = "runCommand:example.renameBadge|{\"targetId\":\"example_badge\",\"text\":\"Updated by command\"}"
```

Example implementation:

```csharp
private static void RenameBadge(IUiStateStore stateStore, IUiCommandContext commandContext) {
    if (!commandContext.Payload.TryDeserialize<TextUpdatePayload>(out var payload) || payload is null)
        return;

    stateStore.Set(badgeTextState, payload.Text);
}
```

## 14. Plugin-owned control state example

Custom plugin controls can also subscribe to `IVisualContext.State` directly when they need behavior beyond the built-in binding set.

Example:

```csharp
private sealed class ExampleBadge : UIElement {
    private string _text;

    public ExampleBadge(IVisualContext context, string id, string? text, string? textStatePath) : base(context, id) {
        _text = string.IsNullOrWhiteSpace(text) ? "Example" : text;

        if (string.IsNullOrWhiteSpace(textStatePath))
            return;

        UiStateKey<string> badgeTextState = new(textStatePath);

        if (Context.State.TryGet(badgeTextState, out var currentText) && currentText is not null)
            _text = currentText;

        TrackDisposable(Context.State.Subscribe(badgeTextState, boundText => {
            if (boundText is not null)
                SetText(boundText);
        }));
    }
}
```

## 15. Typed event payload example

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

## 16. Alternate window example

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

## 17. Current practical recommendations

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

### Prefer typed state keys in code
Prefer `UiStateKey<T>` for code-side state access and subscriptions, then pass `key.Path` into markup binding fields where needed.

### Prefer custom commands for behavior
If the behavior is application- or plugin-specific, prefer:

- `runCommand:...`

instead of endlessly growing the built-in action list.

### Prefer state paths for UI synchronization
If multiple controls or commands need to reflect the same value, prefer shared state paths over direct per-control updates.

## 18. Current limitations

Still worth keeping in mind:

- action argument syntax is still string-based at the outermost level
- merge override detection for frame values still depends on config defaults
- targeting is improved, but still intentionally simple
- bindings currently cover only a small built-in property set
- no full binding/viewmodel layer for markup-defined controls yet
