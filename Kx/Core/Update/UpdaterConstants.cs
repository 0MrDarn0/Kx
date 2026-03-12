// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Update;

public static class UpdaterConstants {
    public const int BufferSize = 8192;
    public const int HttpTimeoutSeconds = 60;
    public const int MaxRetries = 3;
    public const int BaseRetryDelayMs = 1000;
    public const string TempZipPrefix = "kupdater_";
    public const string TempFileSuffixFormat = ".tmp_";
    public const int PooledConnectionLifetimeMinutes = 5;
    public const int PooledConnectionIdleTimeoutMinutes = 2;
    public const int DefaultMaxConnectionsPerServer = int.MaxValue;
    public const string DefaultUserAgent = "kUpdater/1.0";
}
