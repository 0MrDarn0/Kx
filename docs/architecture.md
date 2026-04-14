# Architecture

## High-level structure

The repository contains both a framework and concrete consumers of that framework.

### Projects
- `src/Kx` - framework implementation
- `src/Kx.Sdk` - framework contracts and extension points
- `apps/KxUpdater` - concrete updater application built on the framework
- `apps/KxUpdateBuilder` - desktop manifest builder for the file-based updater flow
- `apps/KxUpdater/Plugins/KalTheme` - app-specific updater plugin scaffold
- `plugins` - reusable plugins shared across multiple apps
- `examples/Kx.Plugin.Example` - reference plugin
- `examples/Kx.Example.App` - reference host application
- `tests/Kx.Tests` - tests for runtime and framework behavior

## Responsibility boundaries

### `Kx.Sdk`
Contains the contracts that framework consumers and plugins depend on.

Examples:
- plugin interfaces
- UI abstractions
- logging abstractions
- window host abstractions
- DI abstractions
- markup contracts

### `Kx`
Contains reusable runtime and framework behavior.

Examples:
- runtime startup and shutdown
- window lifecycle and rendering
- markup loading and control creation
- plugin loading and plugin runtime policy
- logging implementation
- generic YAML configuration loading
- generic asset path and resource loading infrastructure

### `KxUpdater`
Contains concrete application behavior for the updater host.

Examples:
- updater-specific configuration types
- updater-owned asset files under `Assets`
- the concrete updater main window
- file-by-file manifest comparison and download behavior
- application startup entry point

### `KxUpdateBuilder`
Contains the companion publishing tool for the updater host.

Examples:
- desktop UI for selecting `Update` and `Upload` folders
- generating `update.json`
- mirroring files into the publish folder
- tracking deleted files between manifests

### `plugins`
Contains reusable plugins that are intentionally not tied to a single application.

### `apps/<App>/Plugins`
Contains plugins that belong to one concrete application and should evolve beside that app.

### `examples`
Contains learning and reference material.

Examples:
- a sample plugin
- a minimal host application
- runnable demonstration code that should not define framework architecture

## Runtime bootstrap

The main runtime entry point is `Kx.App.Runtime`.

Startup flow:
1. create the runtime object
2. create runtime composition objects
3. register framework defaults into the DI container
4. apply app-specific service registrations
5. start plugin lifecycle
6. show the configured window

## Composition objects

The runtime is intentionally composed from explicit building blocks.

### `PluginRuntimeComposition`
Builds the shared plugin services for one runtime instance.

Contains:
- plugin diagnostics
- plugin registry service
- plugin compatibility policy
- plugin dependency resolver
- plugin instance loader
- plugin loader service
- plugin runtime policy

### `RuntimeUiComposition`
Builds shared UI registries and built-in markup/control registrations.

Contains:
- markup action registry
- UI command registry
- UI state store
- control registry
- theme registry
- window registry

### `RuntimeLoggingComposition`
Builds shared logging services.

Contains:
- debug log sink
- rolling file log sink
- logger factory creation
- system logger creation

### `RuntimeShellComposition`
Builds the remaining shell-facing runtime services.

Contains:
- startup manager creation
- shutdown manager creation
- shared tray icon configuration

## Runtime service registration

`RuntimeServiceConfiguration` registers already composed services into the container.

This file is no longer the main construction site for runtime objects. Its job is now wiring, not building.

## Window architecture

The main base class is `Kx.App.Window`.

It is responsible for:
- creating the `WindowContext`
- loading the window definition
- loading the matching theme
- building configured controls from YAML/registry data
- resolving the window icon from code, markup, or runtime config
- creating renderer and interaction infrastructure
- dispatching window lifecycle events

`Window` also tracks:
- `HasConfiguredControls`
- `HasConfiguredContentControls`

That distinction matters for fallback UI behavior.

## Config and asset boundaries

Framework-generic loading infrastructure remains in `Kx`.

Concrete asset files and concrete app config DTOs belong to the owning app project.

Current examples:
- `Kx.Core.Configuration.ConfigLoader` is generic infrastructure
- updater-specific `AppConfig` lives in `KxUpdater.Configuration`
- updater app asset files now live in `apps/KxUpdater/Assets`
- updater-specific plugins can live under `apps/KxUpdater/Plugins`
- reusable plugins can live under `plugins`
- sample app asset files now live in `examples/Kx.Example.App/Assets`
- `Kx` no longer ships updater-specific `app.yaml`, `frame.yaml`, or language YAML files

## Updater publication model

The updater stack now uses a file-based publication model.

Published update content consists of:
- `update.json`
- `news.yaml`
- the current file tree under the update base URL

`update.json` currently carries:
- `files` with relative paths and SHA256 hashes
- `deletedFiles` for client-side cleanup

`KxUpdater` compares the manifest against the local installation and downloads only the files that differ.

`KxUpdateBuilder` produces that publish layout by mirroring the current `Update` folder into `Upload` and writing the manifest there.

## Why this structure exists

The recent extraction work aims to make the code easier to understand and evolve.

Benefits:
- clearer framework versus app boundaries
- easier separation between reusable and app-specific plugins
- easier testing of runtime subsystems
- smaller responsibilities per class
- easier onboarding for plugin authors and contributors
