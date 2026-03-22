# Windows and markup

## Base window model

All application windows derive from `Kx.App.Window`.

The base class handles:
- creating `WindowContext`
- loading the window definition
- loading an optional theme
- building configured controls
- creating the renderer
- creating interaction logic
- wiring window lifecycle events

## Window definition lookup

A window resolves its configuration in this order:
1. ask `IWindowRegistry` for a named window definition
2. fall back to loading YAML from `WindowConfigPath`

The default window definition name is the CLR type name.

The default path still resolves inside the runtime output `Assets/Configs` layout, but the owning app is now responsible for shipping the actual `frame.yaml` file.

## Themes

A window can reference a theme by name.

If the window config specifies a theme and that theme exists in `IThemeRegistry`, the theme is merged into the final frame and control configuration.

## Configured controls

Configured controls are created through:
- `WindowDefinitionMerger.MergeControls(...)`
- `ControlFactory.Create(...)`
- `IControlRegistry`

This means a window definition can contain both built-in controls and plugin-provided controls.

Buttons can now declare explicit state images in markup instead of using the old ambiguous `skinKey` concept.

Examples:
- `normalImage: "Themes:KalOnline:Buttons:btn_exit.normal.png"`
- `hoverImage: "Themes:KalOnline:Buttons:btn_exit.hover.png"`
- `pressedImage: "Themes:KalOnline:Buttons:btn_exit.pressed.png"`

## Window icons

Window icons can now be supplied from three places.

Precedence:
1. code override via `WindowIconResource`
2. merged frame markup via `frame.default.icon`
3. app-level default via `Assets/Configs/app.yaml` -> `window.icon`

The WinForms host no longer knows a hardcoded icon path. It only applies the icon stream passed down by the runtime.

## Asset id to file path mapping

Resource ids now resolve directly under the app `Assets` root.

Examples:
- `Icons:app.ico` -> `Assets/Icons/app.ico`
- `Themes:KalOnline:Frame:top_left.png` -> `Assets/Themes/KalOnline/Frame/top_left.png`
- `Themes:KalOnline:Buttons:btn_exit.normal.png` -> `Assets/Themes/KalOnline/Buttons/btn_exit.normal.png`

This keeps app-global assets simple while grouping theme-owned visuals under `Assets/Themes/...` without reintroducing the old extra `Assets/Resources/...` nesting layer.

Current concrete layouts:
- updater app: `Assets/Icons/app.ico` plus `Assets/Themes/KalOnline/Frame/...` and `Assets/Themes/KalOnline/Buttons/...`
- example app: `Assets/Icons/app.ico`

## Control layers

Controls can live on different visual layers.

Current layers:
- `Frame`
- `Content`
- `Overlay`

A configured control does not need to be on the `Content` layer to count as real configured UI.

## Fallback UI behavior

The base window now tracks two separate states:
- `HasConfiguredControls`
- `HasConfiguredContentControls`

### Meaning
- `HasConfiguredControls` means at least one configured control was created at all
- `HasConfiguredContentControls` means at least one configured control was created on the `Content` layer

### Why the distinction matters

Hard-coded UI built in a window class should normally be a fallback, not something that overlays already configured YAML/plugin UI.

That means a fallback like `BuildUI()` should usually check `HasConfiguredControls`, not only `HasConfiguredContentControls`.

## Practical rule for window authors

Use code-built UI only when there is no configured UI.

Typical pattern:
- if configured controls exist, do nothing extra
- if none exist, build a simple fallback layout in code

## Built-in UI infrastructure

`RuntimeUiComposition` provides shared registries for the runtime.

It builds:
- `MarkupActionRegistry`
- `UiCommandRegistry`
- `UiStateStore`
- `ControlRegistry`
- `ThemeRegistry`
- `WindowRegistry`

It also registers built-in:
- markup actions
- control factories

## App-owned assets

`Kx` now keeps only the generic path and resource-loading infrastructure.

Concrete apps and examples own the actual files under their local `Assets` folder, for example:
- `Assets/Configs/app.yaml`
- `Assets/Configs/frame.yaml`
- `Assets/Languages/lang_en.yaml`
- `Assets/Languages/lang_de.yaml`
- `Assets/Icons/app.ico`
- `Assets/Themes/<ThemeName>/...`
- app-specific images and other resource files

## Markup-driven interaction

A configured control can bind to:
- commands
- actions
- state values
- theme data

This allows YAML and plugins to drive a window without changing the concrete window class.

## Example behavior

`examples/Kx.Example.App/MainWindow.cs` treats its code-built UI as a real fallback.

That means:
- configured YAML or plugin controls present -> no hard-coded backup UI is added
- no configured controls present -> the fallback code builds a backup interface

## Recommended usage model

For framework users:
- use YAML window definitions for normal window structure
- use themes for frame and visual overrides
- use plugins for custom controls, actions, and commands
- keep code-built UI as a fallback or for temporary scaffolding
