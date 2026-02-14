// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.Update;

public class UpdateMetadata {
    public string Version { get; set; } = "";
    public string PackageUrl { get; set; } = "";
    public UpdateFile[] Files { get; set; } = [];
}

public class UpdateFile {
    public string Path { get; set; } = "";
    public string Sha256 { get; set; } = "";
}
