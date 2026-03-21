# Plugins

## Plugin model

Plugins extend the runtime through `Kx.Sdk.Plugin`.

The core entry point is `IPlugin`.

A plugin can:
- initialize against the runtime service container
- register UI-related behavior into shared registries
- contribute themes and window definitions
- use shared state, commands, and markup actions

If a plugin also needs to register services before the container is built, it can implement `IServicePlugin`.

## Plugin runtime flow

At runtime, plugin handling is split across dedicated components.

### Discovery and loading
- `PluginLoaderService` orchestrates plugin discovery and activation
- `PluginDiscovery` finds plugin folders, manifests, and assemblies
- `PluginCompatibilityPolicy` validates API compatibility
- `PluginDependencyResolver` computes load order
- `PluginInstanceLoader` loads plugin assemblies and creates instances
- `PluginRegistryService` tracks loaded plugin instances and unload order

### Startup policy
`PluginRuntimePolicy` coordinates:
- service registration for service plugins
- plugin initialization order
- plugin shutdown order
- unload behavior on failures

### Diagnostics
`PluginDiagnostics` centralizes trace and error output for plugin loading and runtime events.

## What a plugin typically does

A typical plugin `Initialize` method resolves shared registries from the service container and registers additions.

Common registrations:
- `IControlRegistry`
- `IMarkupActionRegistry`
- `IUiCommandRegistry`
- `IUiStateStore`
- `IThemeRegistry`
- `IWindowRegistry`

## Example plugin walkthrough

The example plugin lives in `examples/Kx.Plugin.Example/Example.cs`.

It demonstrates:
- writing initial UI state values
- registering a custom control named `ExampleBadge`
- registering a custom markup action
- registering a UI command
- loading themes from YAML
- loading window definitions from YAML

The sample app in `examples/Kx.Example.App` is the simplest host that demonstrates how this plugin is consumed.

## Markup assets inside a plugin

The example plugin loads files from a `Markup` folder next to its output assembly.

Typical asset kinds:
- `Themes/*.yaml`
- `Windows/*.yaml`

The plugin builds those paths relative to `typeof(Example).Assembly.Location`.

## Recommended plugin author flow

1. implement `IPlugin`
2. resolve the required registries from `context.Services`
3. register controls, actions, commands, themes, and windows
4. keep plugin assets under a predictable `Markup` folder
5. use `IUiStateStore` for state-driven UI behavior
6. log important plugin lifecycle events through `context.Logger`

## Service registration versus runtime initialization

Use `IServicePlugin` only when the plugin must add services before the DI container is built.

Use plain `IPlugin` when the plugin only needs runtime registries and already-built services.

## Failure handling

The runtime unloads plugins when:
- service registration fails
- plugin initialization fails

This keeps one broken plugin from poisoning the entire runtime startup path.

## Practical guidance

Good first plugin features:
- one custom control
- one action
- one command
- one YAML window
- one YAML theme

That is enough to understand the whole extension model without reading the entire framework first.
