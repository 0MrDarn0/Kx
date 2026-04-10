# Kx Repository — Copilot Onboarding

## Purpose
- Brief: Kx is a small framework and updater application used to build Windows launcher/updater apps and themes/plugins. It contains the runtime (`src/Kx`), a public SDK (`src/Kx.Sdk`), example apps, plugins and an updater application (`apps/KxUpdater`). Tests live under `tests`.

## Quick summary
- What this repo does: provides a WinForms/Skia-based UI runtime, an SDK for plugin authors, example apps and an updater application that downloads and extracts update packages. The KxUpdater app ships plugins and YAML-based UI markup/themes.

## Tech stack
- .NET 10 (net10.0 / net10.0-windows...) — Windows-focused projects use WinForms APIs
- SkiaSharp for rendering
- YamlDotNet for configuration/markup
- xUnit + Microsoft.NET.Test.Sdk for unit tests
- MSBuild targets in project files handle copying plugin assets and building plugin projects

## Where important files live
- Runtime and SDK
  - `src/Kx/` — runtime implementation (WinForms + Skia integration)
  - `src/Kx.Sdk/` — public SDK surface for plugins
- Apps and plugins
  - `apps/KxUpdater/` — updater app, assets, plugin project(s)
  - `apps/KxUpdateBuilder/` — update builder app
  - `apps/KxUpdater/Plugins/KalTheme/` — sample theme plugin
- Examples
  - `examples/` — sample app and sample plugin
- Tests
  - `tests/Kx.Tests/` — unit tests (xUnit)
- Configs and assets
  - `apps/*/Assets/Configs/*.yaml` and `Markup`/`UI` YAML files for themes and windows
- Project rules and style
  - `.editorconfig` — code style and naming rules
- Documentation
  - `docs/language-key-generation.md` — generated localization key setup and new-project integration

## Build and test (recommended)
- Use PowerShell (repo was developed on Windows):
  - Restore: `dotnet restore` (at repo root or per-project)
  - Build: `dotnet build -c Release` or `dotnet build` (project-level builds are acceptable)
  - Tests: `dotnet test tests/Kx.Tests -c Release`
- Notes:
  - Some projects are Windows-only (WinForms). Run build on Windows or in an environment that supports the Windows SDK.
  - Project files include custom targets that build and copy plugin projects; building top-level app projects (for example `apps/KxUpdater/KxUpdater.csproj` or `examples/Kx.Example.App`) will trigger those targets.
  - Plugin outputs must be copied automatically into the publish directory; matching debug/release build behavior is required.

## Coding guidelines (high-priority, follow these first)
- Respect `.editorconfig` naming and formatting rules (file header, underscore-prefixed private fields, PascalCase public APIs).
- Null checks: use `ArgumentNullException.ThrowIfNull` and `string.IsNullOrWhiteSpace` for string guards.
- Avoid public visibility unless necessary — prefer least exposure.
- Async: follow async/await best practices; end async methods with `Async`; accept and propagate CancellationToken.
- Designer files (`*.designer.cs` / `InitializeComponent`) are serialization-only. Do not introduce control flow, lambdas, or modern constructs there — move logic to the main `.cs` file.
- Keep diffs small and minimal; follow existing project structure for new code and unit tests.
- Follow the two-context WinForms rule: designer files conservative; modern C# for rest of code.
- Ensure runtime error messages/dialog texts are in English.

## Conventions and patterns in this repo
- YAML configuration and UI markup are used extensively. Keep YAML keys camel-cased where existing files do so.
- Plugins are delivered as DLL + `Plugin.yaml` + UI YAML under application-specific asset folders (e.g., `$(AssemblyName)` such as `KxUpdater`) to avoid collisions in deployment directories — check app csproj targets that copy plugin assets into their respective output folders.
- Tests expect the public SDK and runtime projects; register new public APIs in `src/Kx.Sdk` if they are needed by plugin authors.

## Common pitfalls and how to avoid them
- Missing Windows SDK — many projects target `net10.0-windows*`. Build failures on Linux/macOS are expected; run CI and local builds on Windows.
- Preview packages — some projects reference preview package versions. If `dotnet restore` fails because of package feed constraints, use the same SDK version used by the project or run a `dotnet nuget locals --clear all` and retry.
- No top-level solution file: build specific projects when in doubt (for example `dotnet build src/Kx/Kx.csproj`).
- Avoid running repository-wide recursive shell tools that assume POSIX (some builds contain Windows path expectations and MSBuild targets that use backslashes).
- When a terminal command fails in this environment, continue with repository inspection tools and do not stall waiting on the failed command.

## Search tips for agents
- Use repo-aware search (IDE symbols, `find` within repo root) rather than blind network searches.
- Search for YAML under `Assets/` and `Markup/` to find plugin/window definitions.
- Search for `Plugin.yaml` to find plugin entry points and their expected asset layout.
- Search for `PipelineStep` and `Pipeline` attributes to find updater pipeline steps.

## What I looked for (short inventory)
- .editorconfig present — follow it.
- No README.md or CONTRIBUTING.md found in repo root — add documentation before editing public APIs.
- Unit tests under `tests/Kx.Tests` — use them as safety net.
- Custom MSBuild targets in app projects to build and copy plugin projects (see `KxUpdater.csproj` and example app csproj).
- Code uses `YamlDotNet`, `SkiaSharp`, `Microsoft.Extensions.DependencyInjection` (runtime & SDK projects).

## If you need to make changes
- Add unit tests under `tests/Kx.Tests` for behavior changes.
- Update `src/Kx.Sdk` for public surface changes — keep runtime implementation in `src/Kx`.
- Update `apps/*/Assets/*` for art/markup assets and ensure the app csproj copies them to output.

## Notes for maintainers / future agents
- Keep public SDK surface minimal and stable; prefer `src/Kx.Sdk` as the plugin contract.
- When adding new NuGet references, prefer stable releases compatible with .NET 10.
- When touching designer files, follow the designer rules strictly and avoid language features unsupported by the designer.

## Contact points in repo (use these to explore code quickly)
- Entry points: `apps/KxUpdater/` and `examples/Kx.Example.App/`
- Config loader: `src/Kx/Core/Configuration/ConfigLoader.cs`
- Updater pipeline: `src/Kx/Core/Pipeline/Steps/` (e.g., `DownloadAndExtractStep.cs`)
- Logging utilities: `src/Kx/Core/Logging/AsyncLogSink.cs`

## Last checks performed
- Searched for `TODO|HACK|FIXME|XXX` — none found in repository code.
- Confirmed `tests/Kx.Tests` exists and references runtime and SDK projects.

## If a build or restore fails
- Re-run `dotnet restore` and capture full logs.
- If package restore fails for preview packages, try clearing local caches and rerunning:
  - `dotnet nuget locals all --clear`
- If MSBuild plugin copy steps fail, build the plugin project separately first (example: `dotnet build apps/KxUpdater/Plugins/KalTheme/KalTheme.csproj`).

## Short checklist for newcomers
- Use Windows to build/run the apps.
- Start with unit tests: `dotnet test tests/Kx.Tests`.
- Follow `.editorconfig` rules.
- Keep the SDK/runtime separation intact.

-- End of file
