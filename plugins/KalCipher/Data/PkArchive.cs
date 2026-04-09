// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using ICSharpCode.SharpZipLib.Zip;

using Kx.Sdk.Cipher;

namespace KalCipher;

public class PkArchive : IPkArchive {
    public string FilePath { get; }
    public string Password { get; } = "JKSYEHAB#9052";
    public List<IDatEntry> Entries { get; } = [];

    private readonly Encryptor _encryptor;

    public PkArchive(string filePath, string password, Encryptor encryptor) {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
    }

    public async Task LoadAsync() {
        Entries.Clear();
        using var zip = new ZipInputStream(File.OpenRead(FilePath)) { Password = Password };
        int idx = 0;
        for (var entry = zip.GetNextEntry(); entry != null; entry = zip.GetNextEntry()) {
            using var ms = new MemoryStream();
            await zip.CopyToAsync(ms);
            Entries.Add(new DatEntry(idx++, entry.Name, ms.ToArray(), _encryptor));
        }
    }

    public async Task SaveAsync() {
        using var zip = new ZipOutputStream(File.Create(FilePath)) { Password = Password };
        zip.SetLevel(5);
        foreach (var dat in Entries) {
            var entry = new ZipEntry(dat.Name) { DateTime = DateTime.Now };
            zip.PutNextEntry(entry);
            var data = dat.GetEncryptedData();
            await zip.WriteAsync(data, 0, data.Length);
        }
        zip.Finish();
    }
}
