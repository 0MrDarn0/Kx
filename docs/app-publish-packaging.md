# App Publish Packaging

## Goal

The normal `bin/Debug` or `bin/Release` output is a developer build output.
It is not the intended distribution layout.

The intended distribution layout is produced through `publish`.

## Intended shape

The app is published as:

- one main executable
- one external `Assets` folder

Typical result:

- `KxUpdater.exe`
- `Assets/Configs/...`
- `Assets/Languages/...`
- `Assets/Plugins/...`

## Why plugins stay external

The updater uses dynamic plugin discovery and loading from:

- `Assets/Plugins/*`

That means plugin files must remain external at runtime.

So the realistic packaging model is:

- single-file main app
- external assets and plugins

not:

- one single file for the entire system

## Publish profile

`KxUpdater` now includes this publish profile:

- `Properties/PublishProfiles/SingleFileWithAssets.pubxml`

It is configured for:

- `Release`
- `win-x64`
- self-contained deployment
- single-file publish
- compression enabled
- trimming disabled

Trimming stays disabled because the current system uses:

- plugin loading
- reflection-based type activation
- YAML deserialization

## Build vs publish behavior

### Build

Normal build output still goes to the app `TargetDir`.

The example plugin is copied into:

- `Assets/Plugins/Kx.Plugin.Example/...`

for local development and debugging.

### Publish

Publish output goes to the publish directory and uses a dedicated publish copy target.

The example plugin is copied into:

- `Assets/Plugins/Kx.Plugin.Example/...`

inside the publish folder.

The plugin keeps these external files:

- `Kx.Plugin.Example.dll`
- `Kx.Plugin.Example.deps.json`
- `Kx.Sdk.dll`
- `Plugin.yaml`
- `Markup/...`

## Recommended command

From the solution root:

`dotnet publish apps/KxUpdater/KxUpdater.csproj /p:PublishProfile=SingleFileWithAssets`

## Expected result

The publish folder should mainly contain:

- `KxUpdater.exe`
- `Assets/...`

The exact plugin folder still contains plugin runtime files because plugin loading is file-based by design.
