# Plugin UI Framework Overview

## Goal

The current UI framework allows applications and plugins to contribute:

- controls
- windows
- themes
- markup actions

The long-term direction is that plugins can build against `Kx.Abstractions` only, without needing the full `Kx` source.

## Main idea

The system is split into two parts:

### `Kx.Abstractions`
Contains the plugin-facing contracts and shared UI base types.

Important areas:

- `Kx.Abstractions.UI.Elements`
- `Kx.Abstractions.UI.Markup`
- `Kx.Abstractions.UI.Themes`
- `Kx.Abstractions.UI.Actions`
- `Kx.Abstractions.UI.Commands`
- `Kx.Abstractions.UI.Payloads`
- `Kx.Abstractions.UI.State`
- `Kx.Abstractions.UI.VisualTree`

This is the SDK surface a plugin should target.

### `Kx`
Contains the host/runtime implementation.

Important host pieces:

- `Runtime`
- `Window`
- `WindowContext`
- `ControlRegistry`
- `WindowRegistry`
- `ThemeRegistry`
- `MarkupActionRegistry`
- `UiCommandRegistry`
- `UiStateStore`
- `ControlFactory`
- `FrameResource`

`Kx` owns the actual runtime behavior, rendering, window hosting, and registry implementations.

## Runtime startup flow

At startup, `Runtime` creates and registers the framework services.

Important services registered in DI:

- `IMarkupActionRegistry`
- `IUiCommandRegistry`
- `IUiStateStore`
- `IControlRegistry`
- `IThemeRegistry`
- `IWindowRegistry`

Built-in registrations are added during runtime setup:

- built-in markup actions
- built-in controls

After that:

1. plugins are loaded
2. plugins register services and UI contributions
3. the dependency container is built
4. plugins are initialized
5. startup lifecycle runs
6. the main window is created and shown

## Window initialization flow

`Window` is the central bootstrap point for a single window.

When a window is created:

1. a `WindowContext` is created
2. the window resolves its `WindowConfig`
3. an optional registered theme is resolved
4. the frame is built from the resolved `FrameConfig`
5. the renderer is created
6. input interaction is attached
7. configured controls are materialized
8. window-specific fallback UI may run afterwards

### Window definition resolution

The base `Window` currently resolves configuration in this order:

1. `IWindowRegistry` lookup by `WindowDefinitionName`
2. fallback to YAML file loading (`frame.yaml` today)

### Theme resolution

If the resolved `WindowConfig` specifies a theme name:

1. `IThemeRegistry` is queried
2. if found, the theme provides the base `Frame` and base `Controls`
3. the window `Frame` overrides theme values when the window values differ from the config type defaults
4. controls are merged by `Id`, including nested child controls
5. unmatched window controls are appended after the themed controls

If no theme is found, the window falls back to the frame defined in `WindowConfig`.

This means themes act as reusable defaults, while window definitions can override only the specific frame values or control nodes they need to change.

## Shared UI model

The shared UI model now lives largely in `Kx.Abstractions`.

Important types:

- `IVisual`
- `Visual`
- `UIElement`
- `IVisualContext`
- `IUIElementManager`
- `VisualLayer`
- `Thickness`
- `Dock`

This makes it possible for plugins to derive from the same base types as the host.

## Registries

### `IControlRegistry`
Used to register control factories by markup type name.

Examples:

- `Label`
- `Button`
- `Grid`
- `DockPanel`
- `StackPanel`
- custom plugin controls like `ExampleBadge`

### `IThemeRegistry`
Used to register named themes.

A theme can currently contribute:

- `Frame`
- `Controls`

### `IWindowRegistry`
Used to register named window definitions.

A window definition can currently contribute:

- `Theme`
- `Frame`
- `Controls`

### `IMarkupActionRegistry`
Used to register named actions that can be triggered from markup.

Built-in actions currently include:

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

Plugins can add custom actions too.

### `IUiCommandRegistry`
Used to register named commands that can be executed from markup through the built-in `runCommand` action.

Commands are useful when markup should trigger application or plugin behavior without adding more dedicated built-in actions.

`IUiCommandContext` now also exposes `Payload`, which supports typed deserialization of JSON command payloads while keeping the raw string argument available.

### `IUiStateStore`
Used to store named UI state values and notify bindings when a state path changes.

`IVisualContext` exposes the active state store through `State`, which allows both host controls and plugin controls to react to shared state paths.

Typed helper keys are available through `UiStateKey<T>`, which reduces repeated raw string paths in plugin and host code while keeping the markup-facing binding fields string-based.

### Shared payload schemas
Common payload DTOs now live in `Kx.Abstractions.UI.Payloads`.

Current shared schemas include:

- `TargetPayload`
- `TextUpdatePayload`
- `VisibilityPayload`
- `OpenWindowPayload`
- `EnabledStatePayload`
- `ColorUpdatePayload`
- `EventPublishPayload`

These types are intended to reduce plugin-local payload duplication and keep built-in actions, commands, and published events aligned on common JSON shapes.

Target-capable payloads accept the same target expressions as markup actions, including `self`, `parent`, `root`, and `id:<value>`.

## Control materialization

`ControlFactory` is responsible for creating UI trees from `ControlConfig`.

It currently applies:

- layer
- margin
- padding
- dock
- fixed bounds
- grid row/column metadata
- grid row/column definitions
- children for nested control trees
- button click actions via `IMarkupActionRegistry`
- command bridging via `runCommand`
- state-driven bindings for text, color, visibility, enabled state, font size, and stack-panel layout properties

This means markup definitions can now build nested control hierarchies, not only flat root controls.

## Markup model

Important config types:

- `ControlConfig`
- `WindowConfig`
- `WindowTheme`
- `BoundsConfig`
- `ThicknessConfig`
- `GridLengthConfig`
- `GridRowConfig`
- `GridColumnConfig`
- `FrameConfig`
- `DefaultFrameConfig`

A `ControlConfig` can now describe:

- visual type
- text/color/font
- bounds
- padding/margin
- docking
- grid placement
- nested children
- action binding via `OnClick`
- state bindings via `TextBinding`, `ColorBinding`, `VisibleBinding`, and `EnabledBinding`
- additional bindings via `FontSizeBinding`, `OrientationBinding`, and `SpacingBinding`

## State and bindings

The current binding layer is intentionally small and path-based.

In code, state access can now use either:

- raw string paths
- typed `UiStateKey<T>` helpers

Available binding fields on `ControlConfig`:

- `TextBinding`
- `ColorBinding`
- `VisibleBinding`
- `EnabledBinding`
- `FontSizeBinding`
- `OrientationBinding`
- `SpacingBinding`

Bindings use the shared `IUiStateStore`.

Value conversion for the built-in bindings is centralized in host-side state conversion helpers instead of being repeated inline per binding.

Current built-in binding support includes:

- `Label.Text`
- `Label.Color`
- `Label.Font` size
- `UIElement.Visible`
- `Button.Text`
- `Button.IsEnabled`
- `Button.FontSize`
- `StackPanel.Orientation`
- `StackPanel.Spacing`

Direct values in `ControlConfig` still act as defaults. If a state path already has a value, the binding overwrites the direct value at runtime.

## Actions

Actions are triggered from markup, currently mainly from buttons.

Format today:

- `actionName`
- `actionName:argument`

Supported target expressions for built-in actions and the shared target resolver:

- `self`
- `parent`
- `root`
- `id:example_badge`
- `example_badge` (legacy plain-id fallback)

Examples:

- `closeWindow`
- `openWindow:Example.Alternate`
- `enable:id:example_toggle_button`
- `disable:{"targetId":"id:example_toggle_button","enabled":false}`
- `focus:id:example_toggle_button`
- `show:id:example_badge`
- `hide:id:example_badge`
- `setColor:{"targetId":"id:example_title","color":"#FFD166"}`
- `setText:id:example_title|Updated by markup`
- `toggleVisibility:id:example_badge`
- `publishEvent:SomethingHappened`
- `publishEvent:BadgeUpdated|{"targetId":"example_badge","text":"Updated by event"}`
- `runCommand:example.renameBadge|{"targetId":"example_badge","text":"Updated by command"}`
- `example.toggleVisibility:id:example_badge`

A plugin can register a custom action and then reference it from `ControlConfig.OnClick`.
A plugin can also register a custom command and trigger it through `runCommand`, then deserialize a typed payload from `context.Payload`.

Published UI events also support typed payloads through `MarkupActionEvent.Payload`, so consumers can deserialize JSON payloads in the same style as UI commands.

Built-in actions also understand the shared payload schemas when JSON is supplied instead of the legacy string form.

Example consumer:

`context.VisualContext.Events.Register<MarkupActionEvent>(e => { if (e.EventName == "BadgeUpdated" && e.Payload.TryDeserialize<TextUpdatePayload>(out var payload)) { /* use payload */ } });`

## Example plugin

`Kx.Plugin.Example` demonstrates the current extension points.

It currently registers:

- a custom control: `ExampleBadge`
- a custom action: `example.toggleVisibility`
- a theme: `Example.Dark`
- a window definition for `MainWindow`
- a custom command: `example.renameBadge` using `TextUpdatePayload`

The example theme contributes a nested control tree:

- `StackPanel`
  - `Label`
  - `ExampleBadge`
  - `Button`
  - additional buttons for built-in actions including enable/disable/focus/setColor

The example now demonstrates both custom and built-in actions, a custom command executed through `runCommand` with the shared `TextUpdatePayload` schema, the new target-resolution syntax, a `MainWindow` definition that overrides parts of the registered theme through merge rules, and a first state-driven binding flow.

## Automatic plugin deployment during build

`Kx.Update.App.csproj` currently copies the example plugin automatically into the app output folder after build.

That ensures these files are placed into:

`Assets\Plugins\Kx.Plugin.Example`

Copied artifacts:

- `Kx.Plugin.Example.dll`
- `Kx.Plugin.Example.pdb`
- `Plugin.yaml`

This was important because stale plugin copies in the app output previously caused runtime confusion.

## Current limitations

The system works, but it is still early framework infrastructure.

Current notable limitations:

- action arguments are still string-based
- command payloads and published event payloads are available as typed JSON, but action arguments are still string-based
- targeting is basic and id-based
- shared payload schemas exist, but richer validation/helper layers are still minimal
- bindings are currently path-based and support only a small set of built-in properties
- theme/window merging currently relies on config defaults to detect window overrides, so explicitly overriding back to a schema default value is still limited
- no general data binding layer for markup-defined controls yet

## Recommended next steps

Reasonable next framework steps are:

1. improve binding ergonomics
   - better converters
   - stronger typed state helpers
   - less stringly binding paths
2. document example markup files once real theme/window YAML usage expands

## Summary

The framework currently supports plugin-driven:

- UI base types
- custom controls
- themes
- window definitions
- nested control trees
- markup actions
- command bridge via `runCommand`

The most important architectural point is:

- contracts live in `Kx.Abstractions`
- runtime implementation lives in `Kx`
- plugins can increasingly work against `Kx.Abstractions` only
