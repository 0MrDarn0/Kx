# Update builder

`KxUpdateBuilder` is the companion publishing tool for the file-based updater flow used by `KxUpdater`.

It does not create a single `update.zip` package anymore. Instead, it mirrors the current update files into an `Upload` folder and writes an `update.json` manifest that describes the published file set.

## Current output model

The publish folder is expected to contain:
- `update.json`
- `news.yaml`
- all published files in their relative folder structure

Example layout:

- `Upload/update.json`
- `Upload/news.yaml`
- `Upload/data/Hypertext/B(+).bmp`
- `Upload/bin/game.dll`

`KxUpdater` downloads files directly from that structure by combining the updater base URL with each manifest path.

## What the builder does

When you click `Build Manifest`, `KxUpdateBuilder`:
1. reads the source files from the `Update` folder
2. mirrors them into the `Upload` folder
3. compares the new file list with the previous `update.json`
4. writes removed paths into `deletedFiles`
5. deletes legacy `update.zip` and `version.txt` artifacts from `Upload`
6. writes the new `update.json`

## Builder UI

The desktop UI currently exposes:
- `Update folder`
- `Upload folder`
- `Existing output` toggle
- `Build Manifest`
- output log panel

### Fields

#### `Update folder`
The source folder that contains the files you want to publish.

#### `Upload folder`
The publish folder that will be uploaded to the web server.

#### `Existing output`
Controls whether an existing `update.json` in the upload folder may be replaced.

## Typical usage

1. copy the new client files into `Update`
2. ensure `news.yaml` is present in `Upload` if you want updater news
3. start `KxUpdateBuilder`
4. verify the `Update folder` and `Upload folder`
5. choose whether existing output may be overwritten
6. click `Build Manifest`
7. upload the resulting `Upload` folder contents to the update server

## Manifest shape

The current `update.json` format is file-based.

Example:

```json
{
  "version": "",
  "packageUrl": "",
  "files": [
    {
      "path": "data/Hypertext/B(+).bmp",
      "sha256": "200014AD39D07ED7A19B88634316068C309C0E36DF102454E8D82C93E9D1BDFD"
    }
  ],
  "deletedFiles": [
    "obsolete/file.txt"
  ]
}
```

### Notes

- `version` is currently optional and can remain empty.
- `packageUrl` is kept for compatibility with the shared metadata type and is currently unused by the file-based flow.
- `files` contains the relative download paths and SHA256 hashes.
- `deletedFiles` tells the updater which local files should be removed.

## Server requirements

The update server must expose static files directly from the published folder structure.

That includes at least:
- `update.json`
- `news.yaml`
- all file paths listed in `files`

### IIS notes

For IIS, static content mappings are required for the file types you publish.

A working setup for this repository includes mappings such as:
- `.json`
- `.yaml`
- `.yml`
- `.txt`
- `.bmp`
- other game-specific binary formats as needed

If published file names contain special characters like `+`, IIS may reject the request with `404.11` unless request filtering allows double escaping.

Relevant setting:

```xml
<security>
  <requestFiltering allowDoubleEscaping="true" />
</security>
```

This was required for files such as `data/Hypertext/B(+).bmp`.

## Practical publishing checklist

- build the manifest after every file set change
- upload the entire `Upload` folder contents, not only `update.json`
- keep `news.yaml` beside `update.json`
- verify special-character file names directly in the browser if downloads fail
- check IIS logs for `sc-status` and `sc-substatus` when a specific file cannot be downloaded

## Related files

- builder UI: `apps/KxUpdateBuilder/MainWindow.cs`
- builder logic: `apps/KxUpdateBuilder/UpdatePackageBuilder.cs`
- updater apply step: `src/Kx/Core/Pipeline/Steps/DownloadAndApplyStep.cs`
- updater metadata model: `src/Kx.Sdk/Updater/UpdateMetadata.cs`
