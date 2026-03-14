// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Kx.Sdk.Updater;



class UpdateBuilder {
    static void Main(string[] args) {
        string updateFolder = Path.Combine(Directory.GetCurrentDirectory(), "Update");
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Upload");

        // Ensure Upload folder exists
        if (!Directory.Exists(uploadFolder)) {
            Directory.CreateDirectory(uploadFolder);
            Console.WriteLine("Upload folder created.");
        }

        string outputZip = Path.Combine(uploadFolder, "update.zip");
        string outputJson = Path.Combine(uploadFolder, "update.json");
        string versionFile = Path.Combine(uploadFolder, "version.txt");

        // Check and create Update folder if missing
        if (!Directory.Exists(updateFolder)) {
            Directory.CreateDirectory(updateFolder);
            Console.WriteLine("Update folder not found. New folder created.");
        }

        // Check if Update folder is empty
        if (Directory.GetFiles(updateFolder, "*", SearchOption.AllDirectories).Length == 0) {
            Console.WriteLine("Wait until update folder is filled. Hit ENTER when ready.");
            Console.ReadLine();
        }

        // Ask for PackageUrl
        string packageUrl;
        while (true) {
            Console.WriteLine("Please enter the PackageUrl (must be a valid http/https URL):");
            packageUrl = Console.ReadLine()?.Trim() ?? "";

            if (IsValidUrl(packageUrl))
                break;

            Console.WriteLine("Invalid URL. Please enter a valid domain or IP with http/https.");
        }

        // Ask once if ZIP already exists
        if (File.Exists(outputZip)) {
            Console.WriteLine($"File '{Path.GetFileName(outputZip)}' already exists. Overwrite all update files? (y/n, default = n)");
            string? answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(answer) || (answer != "y" && answer != "yes")) {
                Console.WriteLine("Aborted. No files overwritten.");
                return;
            }
            // If yes, delete old files
            File.Delete(outputZip);
            if (File.Exists(outputJson))
                File.Delete(outputJson);
            if (File.Exists(versionFile))
                File.Delete(versionFile);
        }

        // 1. Create ZIP
        ZipFile.CreateFromDirectory(updateFolder, outputZip, CompressionLevel.Optimal, includeBaseDirectory: false);
        Console.WriteLine("ZIP created: " + outputZip);

        // 2. Compute hashes
        var files = new List<UpdateFile>();
        foreach (var file in Directory.GetFiles(updateFolder, "*", SearchOption.AllDirectories)) {
            string relativePath = Path.GetRelativePath(updateFolder, file);
            string hash = ComputeSha256(file);
            files.Add(new UpdateFile { Path = relativePath.Replace("\\", "/"), Sha256 = hash });
        }

        // 3. Version (no increment, just keep or default)
        string version = "1.0.0";
        if (File.Exists(versionFile)) {
            version = File.ReadAllText(versionFile).Trim();
            if (string.IsNullOrWhiteSpace(version))
                version = "1.0.0";
        }
        File.WriteAllText(versionFile, version);

        // 4. Write JSON
        var metadata = new UpdateMetadata
        {
            Version = version,
            PackageUrl = packageUrl,
            Files = files
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(outputJson, JsonSerializer.Serialize(metadata, options));

        Console.WriteLine("JSON created: " + outputJson);
        Console.WriteLine("Version: " + version);
        Console.WriteLine("All files written to Upload folder.");
    }

    private static string ComputeSha256(string filePath) {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    private static bool IsValidUrl(string url) {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
