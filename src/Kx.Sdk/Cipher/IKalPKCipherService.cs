// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Sdk.Cipher;

public interface IDatEntry {
    int Index { get; }
    string Name { get; }
    string GetContent();
    void SetContent(string content);
    byte[] GetEncryptedData();
}

public interface IPkArchive {
    string FilePath { get; }
    List<IDatEntry> Entries { get; }
    Task SaveAsync();
}


public interface IKalPKCipherService {
    Task<IPkArchive> LoadArchiveAsync(string filePath, string password);
    Task SaveArchiveAsync(IPkArchive archive);
    string DecryptDat(byte[] data);
    byte[] EncryptDat(string content);
}
