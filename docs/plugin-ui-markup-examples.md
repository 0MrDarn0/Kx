# Plugin UI Markup Examples

## Goal

This document shows practical examples for the current plugin-driven UI framework.

It focuses on:

- themes/frame definitions and content definitions
- merged overrides
- nested controls
- built-in actions
- custom actions
- commands
- state bindings
- typed payloads
- target resolution

For a generic runtime-wide reference (not plugin-only), also see:

- `docs/markup-feature-guide.md`

The example plugin ships YAML assets under:

- `examples/Kx.Plugin.Example/Assets/UI/Frames`
- `examples/Kx.Plugin.Example/Assets/UI/Content`

## 1. Frame definition registration

A frame definition provides reusable defaults for:

- frame styling
- optional reusable controls

Example:

```csharp
frameRegistry.Register("Example.Dark", new WindowFrameDefinition {
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

Real YAML example:

- `examples/Kx.Plugin.Example/Assets/UI/Frames/example_dark_frame.yaml`

## 2. Window content definition registration

A content definition can:

- reference a frame definition via `frameDefinition`
- override parts of the frame
- override controls by `id`
- add additional controls

Example:

```csharp
contentRegistry.Register("MainWindow", new WindowContentDefinition {
    FrameDefinition = "Example.Dark",
    Frame = new FrameConfig {
        Default = new DefaultFrameConfig {
            Title = "Merged Main Window",
            BorderColor = "#FF8C42",
            TitleColor = "#F5F5F5"
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
                    Text = "Added by WindowContentDefinition merge",
                    Color = "#8BD3FF"
                }
            ]
        }
    ]
});
```

Real YAML example:

- `examples/Kx.Plugin.Example/Assets/UI/Content/main_window_content.yaml`

## 3. Merge behavior

Current merge rules are:

### Frame

- frame definition frame is the base
- content frame overrides frame fields
- override detection depends on explicit property assignment
- a content definition can override a frame value back to schema defaults if explicitly assigned

### Controls

- frame controls are the base list
- content controls are matched by `id`
- matching controls are merged
- unmatched content controls are appended
- nested `children` are merged recursively by `id`
- `properties` are overridden key-by-key

## 4. Nested control trees

A `ControlConfig` can contain nested `children`.

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
- `example_badge` (legacy fallback)

Examples:

```csharp
OnClick = "hide:self";
OnClick = "show:parent";
OnClick = "toggleVisibility:id:example_badge";
OnClick = "focus:id:example_toggle_button";
```

## 7. Legacy string action arguments

Some actions still support string forms.

Examples:

```csharp
OnClick = "setText:id:example_title|Updated by markup";
OnClick = "openWindow:Example.Alternate";
OnClick = "publishEvent:SomethingHappened";
```

## 8. State seeding

Plugins can seed initial UI state through `IUiStateStore`.

Preferred code-side usage is via typed keys.

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

## 8a. Loader usage

The example plugin loads frame/content definitions from the deployed plugin folder through `MarkupYamlLoader`.

Example:

```csharp
frameRegistry.Register("Example.Dark", MarkupYamlLoader.Load<WindowFrameDefinition>(GetMarkupPath("Frames", "example_dark_frame.yaml")));
contentRegistry.Register("MainWindow", MarkupYamlLoader.Load<WindowContentDefinition>(GetMarkupPath("Content", "main_window_content.yaml")));
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

Binding expression format:

- `state.path`
- `state.path|converter`
- `state.path|converter|converter:argument`

Current built-in converters:

- `upper`
- `lower`
- `trim`
- `not`
- `default:value`
- `prefix:value`
- `suffix:value`
- `equals:value`

Example:

```csharp
new ControlConfig {
    Type = "Label",
    Id = "example_title",
    Text = "Plugin Window",
    TextBinding = titleState.Path + "|trim|upper|prefix:[BOUND] ",
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

Example inverse visibility binding:

```csharp
new ControlConfig {
    Type = "Label",
    Id = "example_hidden_hint",
    Text = "Shown when merge hint is hidden",
    VisibleBinding = mergeHintVisibleState.Path + "|not"
}
```

Example equality/default pipeline:

```csharp
new ControlConfig {
    Type = "Label",
    Id = "example_status_label",
    TextBinding = statusState.Path + "|trim|default:unknown|equals:ready|upper"
}
```

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

Common payload DTOs live in `Kx.Sdk.UI.Payloads`.

Current shared payloads:

- `TargetPayload`
- `TextUpdatePayload`
- `VisibilityPayload`
- `OpenWindowPayload`
- `EnabledStatePayload`
- `ColorUpdatePayload`
- `EventPublishPayload`

## 11. JSON payload examples for built-in actions

### Show / hide

```csharp
OnClick = "show:{\"targetId\":\"id:example_badge\",\"visible\":true}";
OnClick = "hide:{\"targetId\":\"id:example_badge\",\"visible\":false}";
```

### Enable / disable

```csharp
OnClick = "enable:id:example_toggle_button";
OnClick = "disable:{\"targetId\":\"id:example_toggle_button\",\"enabled\":false}";
```

### Set text

```csharp
OnClick = "setText:{\"targetId\":\"id:example_title\",\"text\":\"Updated by payload\"}";
```

### Set color

```csharp
OnClick = "setColor:{\"targetId\":\"id:example_title\",\"color\":\"#FFD166\"}";
```

### Open window

```csharp
OnClick = "openWindow:{\"windowName\":\"Example.Alternate\"}";
```

## 12. Custom action example

Register action:

```csharp
actionRegistry.Register("example.toggleVisibility", actionContext => ToggleVisibility(actionContext));
```

Use from markup:

```csharp
OnClick = "example.toggleVisibility:id:example_badge";
```

Implementation:

```csharp
private static void ToggleVisibility(IMarkupActionContext actionContext) {
    if (!UiTargetResolver.TryResolve(actionContext.Source, actionContext.Argument, out var visual) || visual is null)
        return;

    visual.Visible = !visual.Visible;
}
```

## 13. Custom command example

Register command:

```csharp
commandRegistry.Register("example.renameBadge", commandContext => RenameBadge(stateStore, commandContext));
```

Invoke from markup:

```csharp
OnClick = "runCommand:example.renameBadge|{\"targetId\":\"example_badge\",\"text\":\"Updated by command\"}";
```

Implementation:

```csharp
private static void RenameBadge(IUiStateStore stateStore, IUiCommandContext commandContext) {
    if (!commandContext.Payload.TryDeserialize<TextUpdatePayload>(out var payload) || payload is null)
        return;

    stateStore.Set(badgeTextState, payload.Text);
}
```

## 14. Plugin-owned control state example

Custom plugin controls can subscribe to `IVisualContext.State` directly.

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

Markup can publish typed payloads:

```csharp
OnClick = "publishEvent:BadgeUpdated|{\"targetId\":\"example_badge\",\"text\":\"Updated by event\"}";
```

Consumer:

```csharp
context.VisualContext.Events.Register<MarkupActionEvent>(e => {
    if (e.EventName == "BadgeUpdated" && e.Payload.TryDeserialize<TextUpdatePayload>(out var payload)) {
        // use payload.TargetId and payload.Text
    }
});
```

## 16. Alternate window/content example

Register alternate content:

```csharp
contentRegistry.Register("Example.Alternate", new WindowContentDefinition {
    FrameDefinition = "Example.Alternate"
});
```

Markup open:

```csharp
OnClick = "openWindow:Example.Alternate";
```

Back-navigation:

```csharp
OnClick = "openWindow:MainWindow";
```

## 17. Current practical recommendations

### Prefer frame definitions for reusable defaults

Put reusable base styling and reusable base controls into frame definitions.

### Prefer content definitions for scenario-specific overrides

Put concrete per-window adjustments into content definitions.

### Prefer shared payload DTOs

Prefer:

- `TextUpdatePayload`
- `VisibilityPayload`
- `OpenWindowPayload`
- `EnabledStatePayload`
- `ColorUpdatePayload`
- `EventPublishPayload`

instead of plugin-local payload records where possible.

### Prefer typed state keys in code

Prefer `UiStateKey<T>` for code-side state access and subscriptions, then pass `key.Path` into markup bindings.

### Prefer custom commands for behavior

If behavior is app- or plugin-specific, prefer `runCommand:...` over endlessly extending built-in actions.

### Prefer shared state paths for synchronization

If multiple controls/commands reflect the same value, prefer shared state paths over direct per-control updates.

## 18. Current limitations

- action argument syntax is still string-based at the outermost level
- target resolution is improved, but intentionally simple
- bindings currently cover a constrained built-in property set
- there is no full MVVM/viewmodel layer for markup-defined controls yet
