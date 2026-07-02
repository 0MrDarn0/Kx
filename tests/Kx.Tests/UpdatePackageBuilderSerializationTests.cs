using System.Reflection;
using System.Text.Json;

namespace Kx.Tests;

public sealed class UpdatePackageBuilderSerializationTests {
    [Fact]
    public void WhenManifestContainsPlusSignThenUpdateJsonKeepsTheLiteralFileName() {
        string workingDirectory = CreateTempDirectory();
        string updateFolder = Path.Combine(workingDirectory, "Update");
        string uploadFolder = Path.Combine(workingDirectory, "Upload");
        Directory.CreateDirectory(Path.Combine(updateFolder, "data", "Hypertext"));
        Directory.CreateDirectory(uploadFolder);

        string sourceFilePath = Path.Combine(updateFolder, "data", "Hypertext", "B(+).bmp");
        File.WriteAllText(sourceFilePath, "bitmap-data");

        BuildUpdatePackage(updateFolder, uploadFolder);

        string updateJsonPath = Path.Combine(uploadFolder, "update.json");
        string json = File.ReadAllText(updateJsonPath);

        Assert.Contains("B(+).bmp", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u002B", json, StringComparison.OrdinalIgnoreCase);

        using JsonDocument document = JsonDocument.Parse(json);
        string manifestPath = document.RootElement
            .GetProperty("Files")[0]
            .GetProperty("Path")
            .GetString() ?? string.Empty;

        Assert.Equal("data/Hypertext/B(+).bmp", manifestPath);
    }

    [Fact]
    public void WhenUpdateFolderContainsThumbsDbThenBuilderExcludesItFromManifestAndUpload() {
        string workingDirectory = CreateTempDirectory();
        string updateFolder = Path.Combine(workingDirectory, "Update");
        string uploadFolder = Path.Combine(workingDirectory, "Upload");
        Directory.CreateDirectory(Path.Combine(updateFolder, "data", "HyperText", "MiniMap"));
        Directory.CreateDirectory(uploadFolder);

        string contentFilePath = Path.Combine(updateFolder, "data", "HyperText", "MiniMap", "map.dat");
        string thumbsFilePath = Path.Combine(updateFolder, "data", "HyperText", "MiniMap", "Thumbs.db");
        File.WriteAllText(contentFilePath, "map-data");
        File.WriteAllText(thumbsFilePath, "shell-cache");

        BuildUpdatePackage(updateFolder, uploadFolder);

        string updateJsonPath = Path.Combine(uploadFolder, "update.json");
        string json = File.ReadAllText(updateJsonPath);

        Assert.DoesNotContain("Thumbs.db", json, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(uploadFolder, "data", "HyperText", "MiniMap", "map.dat")));
        Assert.False(File.Exists(Path.Combine(uploadFolder, "data", "HyperText", "MiniMap", "Thumbs.db")));
    }

    private static void BuildUpdatePackage(string updateFolder, string uploadFolder) {
        Assembly builderAssembly = typeof(KxUpdateBuilder.MainWindow).Assembly;
        Type builderType = builderAssembly.GetType("KxUpdateBuilder.UpdatePackageBuilder", throwOnError: true)!;
        Type requestType = builderAssembly.GetType("KxUpdateBuilder.UpdatePackageBuildRequest", throwOnError: true)!;

        object builder = Activator.CreateInstance(builderType, nonPublic: true)
            ?? throw new InvalidOperationException("Could not create UpdatePackageBuilder.");
        object request = Activator.CreateInstance(requestType, updateFolder, uploadFolder, true)
            ?? throw new InvalidOperationException("Could not create UpdatePackageBuildRequest.");

        MethodInfo buildMethod = builderType.GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMethodException(builderType.FullName, "Build");

        object? result = buildMethod.Invoke(builder, [request]);
        Assert.NotNull(result);
    }

    private static string CreateTempDirectory() {
        string path = Path.Combine(Path.GetTempPath(), "kx-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
