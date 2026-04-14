// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Cipher;

namespace KalCipher;

public class DatEntry : IDatEntry {
    public int Index { get; }
    public string Name { get; }
    private byte[] _data;
    private readonly Encryptor _encryptor;

    public DatEntry(int index, string name, byte[] data, Encryptor encryptor) {
        Index = index;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
    }

    public string GetContent() {
        return _encryptor.Decrypt(_data);
    }

    public void SetContent(string content) {
        _data = _encryptor.Encrypt(content);
    }

    public byte[] GetEncryptedData() => _data;
}
