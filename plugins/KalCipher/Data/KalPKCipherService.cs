// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Cipher;

namespace KalCipher.Data;

public class KalPKCipherService : IKalPKCipherService {
    private readonly Encryptor _encryptor;

    public KalPKCipherService(Encryptor encryptor) {
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
    }

    // Parameterless ctor kept for tests and simple usage; prefers explicit DI in production.
    public KalPKCipherService() : this(Encryptor.Default) { }

    public async Task<IPkArchive> LoadArchiveAsync(string filePath, string password) {
        var archive = new PkArchive(filePath, password, _encryptor);
        await archive.LoadAsync();
        return archive;
    }

    public async Task SaveArchiveAsync(IPkArchive archive) {
        ArgumentNullException.ThrowIfNull(archive);
        await archive.SaveAsync();
    }

    public string DecryptDat(byte[] data) {
        ArgumentNullException.ThrowIfNull(data);
        return _encryptor.Decrypt(data);
    }

    public byte[] EncryptDat(string content) {
        ArgumentNullException.ThrowIfNull(content);
        return _encryptor.Encrypt(content);
    }
}
