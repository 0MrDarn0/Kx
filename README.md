# Kx

`Kx` is a .NET 10 desktop UI/runtime framework with a plugin-driven window system, YAML-based markup, custom rendering, and a sample updater application built on top of it.

The repository now contains three main areas:
- reusable framework and runtime infrastructure in `src`
- tests in `tests`
- runnable reference material in `examples`

## Repository overview

### Framework and app projects
- `src/Kx` - framework runtime, window system, rendering, configuration loading, plugin infrastructure
- `src/Kx.Sdk` - contracts for plugins, UI, logging, DI, window hosting, and markup
- `src/Kx.Update.App` - concrete updater application built on the framework
- `src/Kx.Update.Builder` - update package builder tool
- `tests/Kx.Tests` - framework and runtime tests

### Examples
- `examples/Kx.Plugin.Example` - reference plugin that registers controls, actions, commands, themes, and window definitions
- `examples/Kx.Example.App` - minimal sample host application that demonstrates the framework together with the example plugin

## Current architecture direction

The codebase is being split more clearly into:
- framework-generic infrastructure in `Kx`
- app-specific behavior in `Kx.Update.App`
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
- `docs/plugins.md` - plugin model, registries, markup assets, and example plugin walkthrough
- `docs/windows-and-markup.md` - window lifecycle, YAML/registry lookup, control layers, and fallback UI behavior

## Quick start

### Run the concrete updater app
Open the solution in Visual Studio 2026 or build from the repository root and run `src/Kx.Update.App`.

### Run the sample app
Build and run `examples/Kx.Example.App`. It is intended as the smallest reference host for the framework and the example plugin.

### Main entry points
- updater app startup: `src/Kx.Update.App/Program.cs`
- sample app startup: `examples/Kx.Example.App/Program.cs`
- runtime bootstrap: `src/Kx/App/Runtime.cs`
- base window behavior: `src/Kx/App/Window.cs`
- example plugin: `examples/Kx.Plugin.Example/Example.cs`

## Configuration boundaries

Framework-generic configuration loading stays in `Kx.Core.Configuration`.

Application-specific configuration belongs in the app project. For example:
- updater app config types now live in `Kx.Update.App.Configuration`
- framework-only runtime config such as UI language lives in `Kx.App`

## Goals of the framework

- plugin-driven UI extension
- YAML-defined windows and themes
- custom-rendered desktop UI
- explicit runtime composition
- separation between reusable framework code and app-specific code

## License

This project is licensed under the GPL-3.0. See `LICENSE.txt`.
