// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)


namespace Kx.Sdk.Updater;


public class UpdateFile
{
    public string Path { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
}

public class UpdateMetadata
{
    public List<UpdateFile> Files { get; set; } = [];
    public List<string> DeletedFiles { get; set; } = [];
}
