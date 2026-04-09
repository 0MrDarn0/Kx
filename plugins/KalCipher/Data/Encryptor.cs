// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using System.Text;

using System;
using System.IO;
namespace KalCipher;

public class Encryptor {
    private readonly Lazy<byte[]> _encryptTable = new(() => File.ReadAllBytes("Assets/Table/Encrypt_2006.bin"));
    private readonly Lazy<byte[]> _decryptTable = new(() => File.ReadAllBytes("Assets/Table/Decrypt_2006.bin"));
    private readonly ConcurrentDictionary<int, byte[]> _encryptSlices = new();
    private readonly ConcurrentDictionary<int, byte[]> _decryptSlices = new();
    private readonly Encoding _tableEncoding;

    // e.pk key = 4
    // config.pk key = 8
    public int Key { get; set; } = 8;

    public Encryptor(Encoding tableEncoding) {
        _tableEncoding = tableEncoding ?? throw new ArgumentNullException(nameof(tableEncoding));
    }

    // Default shared instance for convenience (uses CP949 by default)
    public static readonly Encryptor Default;

    static Encryptor() {
        // Ensure code pages are available and create default instance
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Default = new Encryptor(Encoding.GetEncoding(949));
    }


    public byte[] GetTableSlice(byte[] table, int key) {
        int offset = key << 8;
        var slice = new byte[256];
        Array.Copy(table, offset, slice, 0, 256);
        return slice;
    }

    public byte[] Encrypt(string input) {
        var utf = Encoding.UTF8;
        var euc = _tableEncoding;
        var src = utf.GetBytes(input ?? string.Empty);
        var buf = Encoding.Convert(utf, euc, src);

        if (buf.Length == 0)
            return Array.Empty<byte>();

        var slice = _encryptSlices.GetOrAdd(Key, k => GetTableSlice(_encryptTable.Value, k));
        Span<byte> span = buf;
        for (int i = 0; i < span.Length; i++)
            span[i] = slice[span[i]];

        return buf;
    }

    public string Decrypt(byte[] input) {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            return string.Empty;

        var slice = _decryptSlices.GetOrAdd(Key, k => GetTableSlice(_decryptTable.Value, k));

        // Map bytes using the slice
        var output = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
            output[i] = slice[input[i]];

        // Convert from table encoding (EUC/CP949) to UTF-8
        var euc = _tableEncoding;
        var utf = Encoding.UTF8;
        var utfBytes = Encoding.Convert(euc, utf, output);
        return utf.GetString(utfBytes);
    }
}
