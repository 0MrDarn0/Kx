// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.Json;

namespace Kx.Abstractions.UI.Commands;

/// <summary>
/// Represents the raw and typed payload of a UI command.
/// </summary>
public sealed class UiCommandPayload(string? raw) {
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web) {
        PropertyNameCaseInsensitive = true
    };

    public string? Raw { get; } = raw;
    public bool HasValue => !string.IsNullOrWhiteSpace(Raw);

    public bool TryDeserialize<T>(out T? value) {
        value = default;

        if (!HasValue)
            return false;

        try {
            value = JsonSerializer.Deserialize<T>(Raw!, _serializerOptions);
            return value is not null;
        }
        catch (JsonException) {
            value = default;
            return false;
        }
        catch (NotSupportedException) {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Deserializes the payload to the specified type.
    /// </summary>
    public T Deserialize<T>() {
        if (!HasValue)
            throw new InvalidOperationException("The UI command payload is empty.");

        return JsonSerializer.Deserialize<T>(Raw!, _serializerOptions)
            ?? throw new InvalidOperationException($"The UI command payload could not be deserialized to '{typeof(T).Name}'.");
    }
}
