// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using System.Security.Cryptography;

namespace Kx.Utility;

/// <summary>
/// Helfer zum Erzeugen eines stabilen, deterministischen Mutex-Namens (Global\{GUID}) basierend auf
/// Assembly-Informationen oder einem beliebigen Seed-String.
/// Die erzeugte GUID ist name-basiert (SHA-1) und setzt die RFC-entsprechenden Version/Variant-Bits.
/// </summary>
public static class GuidMutex {
    /// <summary>
    /// Erzeugt den vollständigen Mutex-Namen im Format <c>Global\{GUID}</c>.
    /// Standardmäßig wird nur der Assembly-Namen als Seed verwendet; setze <paramref name="useVersion"/> auf <c>true</c>,
    /// um zusätzlich die Assembly-Version in den Seed aufzunehmen (führt zu einer anderen GUID pro Version).
    /// </summary>
    public static string Create(bool useVersion = false) {
        string seed = CreateAssemblySeed(useVersion);
        Guid guid = CreateDeterministicGuidFromString(seed);
        return $"Global\\{{{guid}}}";
    }

    /// <summary>
    /// Erzeugt eine deterministische GUID aus einem beliebigen String.
    /// Implementiert eine name-basierte GUID auf Basis SHA-1 und setzt GUID-Version 5 / Variant bits.
    /// </summary>
    public static Guid CreateDeterministicGuidFromString(string input) {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Seed darf nicht leer sein.", nameof(input));

        // SHA-1 Hash des Inputs
        using var sha1 = SHA1.Create();
        byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Verwende die ersten 16 Bytes des SHA-1 Hashes
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);

        // Setze Version auf 5 (name-based SHA-1)
        bytes[6] = (byte)((bytes[6] & 0x0F) | (5 << 4));
        // Setze RFC 4122 Variant (10xxxxxx)
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
    }

    /// <summary>
    /// Erstellt den Seed aus dem Assembly-Namen oder Assembly-Name + Version.
    /// Fallbacks werden behandelt, falls Assembly-Infos nicht verfügbar sind.
    /// </summary>
    private static string CreateAssemblySeed(bool includeVersion) {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var name = asm.GetName().Name ?? "kxapp";
        if (includeVersion) {
            var ver = asm.GetName().Version?.ToString() ?? "0.0.0.0";
            return $"{name}:{ver}";
        }

        return name;
    }
}
