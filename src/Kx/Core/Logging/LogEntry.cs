// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Logging;

namespace Kx.Core.Logging;

public sealed record LogEntry(
    string Category,
    LogLevel Level,
    string Message,
    Exception? Exception
);
