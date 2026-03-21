# Architecture

## High-level structure

The repository contains both a framework and concrete consumers of that framework.

### Projects
- `src/Kx` - framework implementation
- `src/Kx.Sdk` - framework contracts and extension points
- `src/Kx.Update.App` - concrete updater application built on the framework
- `src/Kx.Update.Builder` - update package builder
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

### `Kx.Update.App`
Contains concrete application behavior for the updater host.

Examples:
- updater-specific configuration types
- the concrete updater main window
- application startup entry point

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
- creating renderer and interaction infrastructure
- dispatching window lifecycle events

`Window` also tracks:
- `HasConfiguredControls`
- `HasConfiguredContentControls`

That distinction matters for fallback UI behavior.

## Config boundaries

Framework-generic config loading remains in `Kx.Core.Configuration`.

Application-specific config DTOs should live in the concrete app project.

Current example:
- `Kx.Core.Configuration.ConfigLoader` is generic infrastructure
- updater-specific `AppConfig` lives in `Kx.Update.App.Configuration`
- framework-only runtime config lives in `Kx.App`

## Why this structure exists

The recent extraction work aims to make the code easier to understand and evolve.

Benefits:
- clearer framework versus app boundaries
- easier testing of runtime subsystems
- smaller responsibilities per class
- easier onboarding for plugin authors and contributors
