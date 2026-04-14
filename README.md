# Kx

`Kx` is a .NET 10 desktop UI/runtime framework with a plugin-driven window system, YAML-based markup, custom rendering, and a sample updater application built on top of it.

The repository now contains five main areas:
- reusable framework and runtime infrastructure in `src`
- concrete applications in `apps`
- reusable plugins in `plugins`
- tests in `tests`
- runnable reference material in `examples`

## Repository overview

### Framework and app projects
- `src/Kx` - framework runtime, window system, rendering, configuration loading, plugin infrastructure
- `src/Kx.Sdk` - contracts for plugins, UI, logging, DI, window hosting, and markup
- `apps/KxUpdater` - concrete updater application built on the framework, including its own `Assets` content and the file-based updater client
- `apps/KxUpdateBuilder` - desktop manifest builder for publishing file-based updates into an `Upload` folder
- `apps/KxUpdater/Plugins/KalTheme` - updater-specific visual plugin scaffold for the KalOnline style and updater-specific UI work
- `tests/Kx.Tests` - framework and runtime tests
- `tests/KxUpdater.Tests` - updater application tests

### Reusable plugins
- `plugins` - home for reusable plugins that are not tied to a single application
- `plugins/KalCipher` - reusable cipher plugin
- `plugins/KalCipher.Tests` - tests for the reusable cipher plugin

### Examples
- `examples/Kx.Plugin.Example` - reference plugin that registers controls, actions, commands, themes, and window definitions
- `examples/Kx.Example.App` - minimal sample host application that demonstrates the framework together with the example plugin and its own local `Assets` content

## Current architecture direction

The codebase is being split more clearly into:
- framework-generic infrastructure in `Kx`
- app-specific behavior in `KxUpdater`
- reusable cross-app plugins under `plugins`
- app-specific plugins beside their owning app
- plugin extension points through `Kx.Sdk`
- learning and reference material under `examples`

The runtime bootstrap is now composed explicitly through dedicated composition objects:
- `PluginRuntimeComposition`
- `RuntimeUiComposition`
- `RuntimeLoggingComposition`
- `RuntimeShellComposition`

This keeps `RuntimeServiceConfiguration` focused on registering already composed services instead of constructing them inline.

## Documentation

- `docs/architecture.md` - project boundaries, runtime startup flow, and composition model
- `docs/grid-splitter.md` - runtime-resizable grid dividers, YAML usage, and layout recommendations
- `docs/plugins.md` - plugin model, registries, markup assets, and example plugin walkthrough
- `docs/update-builder.md` - file-based updater publishing flow, `KxUpdateBuilder` usage, manifest structure, and IIS notes
- `docs/windows-and-markup.md` - window lifecycle, YAML/registry lookup, control layers, icon precedence, and fallback UI behavior

## Quick start

### Run the concrete updater app
Open the solution in Visual Studio 2026 or build from the repository root and run `apps/KxUpdater`.

### Run the update builder
Build and run `apps/KxUpdateBuilder`. It mirrors files from an `Update` folder into an `Upload` folder and writes a file-based `update.json` manifest for `KxUpdater`.

### Run the sample app
Build and run `examples/Kx.Example.App`. It is intended as the smallest reference host for the framework and the example plugin.

### Run tests
- framework/runtime tests: `dotnet test tests/Kx.Tests/Kx.Tests.csproj`
- updater app tests: `dotnet test tests/KxUpdater.Tests/KxUpdater.Tests.csproj`
- reusable plugin tests: `dotnet test plugins/KalCipher.Tests/KalCipher.Tests.csproj`

### Main entry points
- updater app startup: `apps/KxUpdater/Program.cs`
- update builder startup: `apps/KxUpdateBuilder/Program.cs`
- sample app startup: `examples/Kx.Example.App/Program.cs`
- runtime bootstrap: `src/Kx/App/Runtime.cs`
- base window behavior: `src/Kx/App/Window.cs`
- example plugin: `examples/Kx.Plugin.Example/Example.cs`

## File-based update flow

`KxUpdater` now uses a file-based manifest flow instead of requiring a monolithic `update.zip` package.

Current publication model:
- `update.json` describes all current files plus `deletedFiles`
- `news.yaml` provides updater news entries
- each published file is downloaded directly from its relative path under the update base URL

Current client behavior:
- load `update.json`
- compare local file hashes against manifest entries
- download only changed or missing files
- remove files listed in `deletedFiles`
- stage a replacement updater executable for self-update when needed

Current builder behavior:
- read source files from `Update`
- mirror them into `Upload`
- remove legacy `update.zip` and `version.txt` artifacts from `Upload`
- write `update.json`

For a practical walkthrough, see `docs/update-builder.md`.

## Asset and configuration boundaries

Framework-generic path and loading infrastructure stays in `Kx`.

Concrete asset files belong to the owning app or example project. That includes:
- `Assets/Configs/app.yaml`
- `Assets/Configs/frame.yaml`
- `Assets/Languages/*.yaml`
- app-owned icons under `Assets/Icons`
- theme-owned visuals under `Assets/Themes/<ThemeName>/...`

Resource ids map directly under the app `Assets` root. For example:
- `Icons:app.ico` -> `Assets/Icons/app.ico`
- `Themes:KalOnline:Frame:top_left.png` -> `Assets/Themes/KalOnline/Frame/top_left.png`
- `Themes:KalOnline:Buttons:btn_exit.normal.png` -> `Assets/Themes/KalOnline/Buttons/btn_exit.normal.png`

Current examples:
- updater app assets now live under `apps/KxUpdater/Assets`
  - `Assets/Icons/app.ico`
  - `Assets/Themes/KalOnline/Frame/...`
  - `Assets/Themes/KalOnline/Buttons/...`
- sample app assets now live under `examples/Kx.Example.App/Assets`
  - `Assets/Icons/app.ico`
- `Kx` keeps generic asset resolution infrastructure, not updater-specific content files

## Plugin and test structure

- app-local plugins live beside their owning app, for example `apps/KxUpdater/Plugins/KalTheme`
- reusable plugins live under `plugins`, for example `plugins/KalCipher`
- app hosts include plugins through the shared MSBuild `PluginProject` + `build/PluginCopy.targets` pattern
- framework tests stay in `tests/Kx.Tests`
- app-specific tests stay in dedicated app test projects such as `tests/KxUpdater.Tests`
- reusable plugin tests stay beside the plugin under `plugins/<PluginName>.Tests`

## Goals of the framework

- plugin-driven UI extension
- YAML-defined windows and themes
- custom-rendered desktop UI
- explicit runtime composition
- separation between reusable framework code and app-specific code

## License

This project is licensed under the GPL-3.0. See `LICENSE.txt`.
