// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Update;

public interface IUpdateSource {
    Task<string> GetMetadataJsonAsync(string metadataUrl, CancellationToken ct = default);
    Task<Stream> GetPackageStreamAsync(string packageUrl, CancellationToken ct = default);
    Task<long?> GetPackageSizeAsync(string packageUrl, CancellationToken ct = default);
    Task<string> GetChangelogAsync(string changelogUrl, CancellationToken ct = default);
}
