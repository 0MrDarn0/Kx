// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.Json;

namespace Kx.Abstractions.UI.Payloads;

/// <summary>
/// Describes a named event to publish, including an optional JSON payload.
/// </summary>
public sealed record EventPublishPayload(string EventName, JsonElement Payload) {
    public bool HasPayload => Payload.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
    public string? PayloadJson => HasPayload ? Payload.GetRawText() : null;
}
