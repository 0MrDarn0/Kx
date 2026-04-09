# Language key generation

## Purpose

`Kx` and `KxUpdater` use generated strongly typed localization keys instead of handwritten string-id catalogs.

The build-time generator reads the English source language file `lang_en.yaml` and emits a `.g.cs` class with nested `LanguageKey` properties such as:

- `KxLanguageKeys.Dialog.SingleInstance.Title`
- `UpdaterLanguageKeys.Status.WebsiteOpening`

This provides:

- IntelliSense for localization ids
- rename-safe call sites
- fewer string typos in `LanguageService.Translate(...)`
- a single source of truth based on `lang_en.yaml`

## Relevant files

- `tools/Kx.LanguageKeyGenerator/`
  - small console tool that reads YAML and writes generated C#
- `build/LanguageKeyGeneration.targets`
  - shared MSBuild integration used by projects that want generated language keys
- `src/Kx/Assets/Languages/lang_en.yaml`
  - framework source language file for `KxLanguageKeys`
- `apps/KxUpdater/Assets/Languages/lang_en.yaml`
  - app source language file for `UpdaterLanguageKeys`

## How it works

During build, the consuming project sets MSBuild properties describing:

- the source YAML file
- the generated output file
- the generated namespace
- the generated class name
- the generated visibility

The shared target then:

1. restores and builds `tools/Kx.LanguageKeyGenerator`
2. runs the generator with those properties
3. includes the generated `.g.cs` file in the compile

Generated files are written into the consuming project's `obj/<Configuration>/Generated/` folder and are not meant to be edited manually.

## Runtime usage

Use generated keys together with `LanguageService.Translate(...)`:

- `LanguageService.Translate(KxLanguageKeys.Dialog.SingleInstance.Title)`
- `LanguageService.Translate(KxLanguageKeys.Status.UpdateFailed, ex.Message)`
- `LanguageService.Translate(UpdaterLanguageKeys.Status.WebsiteOpening)`
- `LanguageService.Translate(UpdaterLanguageKeys.Button.Start)`

The plain string API still exists, but new code should prefer generated keys where possible.

## Adding language key generation to a new project

A new project needs:

1. an English source language file, typically `Assets/Languages/lang_en.yaml`
2. an import of `build/LanguageKeyGeneration.targets`
3. project properties that describe the generated key catalog
4. a reference to `src/Kx` if the project uses `LanguageKey` and `LanguageService`

### Minimal project setup

Example for an app project:

```xml
<PropertyGroup>
  <LanguageKeySourceFile>$(MSBuildProjectDirectory)\Assets\Languages\lang_en.yaml</LanguageKeySourceFile>
  <LanguageKeyGeneratedFile>$(MSBuildProjectDirectory)\obj\$(Configuration)\Generated\MyAppLanguageKeys.g.cs</LanguageKeyGeneratedFile>
  <LanguageKeysNamespace>MyApp</LanguageKeysNamespace>
  <LanguageKeysClassName>MyAppLanguageKeys</LanguageKeysClassName>
  <LanguageKeysVisibility>internal</LanguageKeysVisibility>
</PropertyGroup>

<Import Project="..\..\build\LanguageKeyGeneration.targets" />
```

Example for a framework/shared project that wants public keys:

```xml
<PropertyGroup>
  <LanguageKeySourceFile>$(MSBuildProjectDirectory)\Assets\Languages\lang_en.yaml</LanguageKeySourceFile>
  <LanguageKeyGeneratedFile>$(MSBuildProjectDirectory)\obj\$(Configuration)\Generated\MyFrameworkLanguageKeys.g.cs</LanguageKeyGeneratedFile>
  <LanguageKeysNamespace>MyFramework.Localization</LanguageKeysNamespace>
  <LanguageKeysClassName>MyFrameworkLanguageKeys</LanguageKeysClassName>
  <LanguageKeysVisibility>public</LanguageKeysVisibility>
</PropertyGroup>

<Import Project="..\..\build\LanguageKeyGeneration.targets" />
```

## Naming guidance

- `lang_en.yaml` is the source of truth for generated key structure
- YAML nesting becomes nested C# classes
- YAML leaf keys become `LanguageKey` properties
- a few common tokens are normalized for readability, for example:
  - `downloading_pkg` -> `DownloadingPackage`
  - `selfupdate_started` -> `SelfUpdateStarted`
  - `invalid_url` -> `InvalidUrl`

## Authoring rules

- Edit `lang_en.yaml`, not the generated `.g.cs` file
- Keep English entries complete because generation is based on the English source file
- Keep YAML key names stable when possible because renames will change generated API names
- Prefer adding tests when introducing new generated key areas that are used by framework or app code

## Validation

The repo currently protects this setup with tests that compare YAML leaf keys against the generated catalogs:

- `tests/Kx.Tests/LanguageKeyCatalogTests.cs`
- `tests/Kx.Tests/LanguageLoaderTests.cs`

If a YAML key disappears from the generated catalog or a generated name collides badly enough to lose coverage, those tests should fail.
