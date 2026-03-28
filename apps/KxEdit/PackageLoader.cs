using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace KxEdit;

internal static class PackageLoader {
    public static string LoadAsText(string path) {
        using var fs = File.OpenRead(path);

        if (IsZip(fs)) {
            fs.Seek(0, SeekOrigin.Begin);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: true);
            var sb = new StringBuilder();
            foreach (var entry in archive.Entries) {
                if (IsTextEntry(entry.FullName)) {
                    sb.AppendLine($"--- {entry.FullName} ---");
                    using var sr = new StreamReader(entry.Open(), Encoding.UTF8);
                    sb.AppendLine(sr.ReadToEnd());
                }
            }

            return sb.ToString();
        }

        fs.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(fs, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static bool IsZipStream(Stream s) => IsZip(s);

    private static bool IsZip(Stream s) {
        if (!s.CanSeek)
            return false;

        var original = s.Position;
        try {
            s.Seek(0, SeekOrigin.Begin);
            Span<byte> hdr = stackalloc byte[4];
            int read = s.Read(hdr);
            if (read < 4) return false;
            // PK..
            return hdr[0] == 0x50 && hdr[1] == 0x4B;
        }
        finally {
            s.Seek(original, SeekOrigin.Begin);
        }
    }

    private static bool IsTextEntry(string name) {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext == ".txt" || ext == ".cfg" || ext == ".ini" || ext == ".yaml" || ext == ".yml" || ext == ".json";
    }
}
