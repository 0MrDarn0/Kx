// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text;

namespace Kx.LanguageKeyGenerator;

internal static class Program {
    private static int Main(string[] args) {
        if (args.Length != 5) {
            Console.Error.WriteLine("Expected arguments: <input-yaml> <output-cs> <namespace> <class-name> <visibility>.");
            return 1;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        string @namespace = args[2];
        string className = args[3];
        string visibility = args[4];

        try {
            string yaml = File.ReadAllText(inputPath, Encoding.UTF8);
            string generatedCode = LanguageKeyCodeGenerator.Generate(yaml, @namespace, className, visibility, inputPath);

            string? outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(outputPath, generatedCode, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return 0;
        }
        catch (Exception ex) {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
