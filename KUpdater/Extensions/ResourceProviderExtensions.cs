// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Utility;
using SkiaSharp;

namespace KUpdater.Extensions;

public static class ResourceProviderExtensions {
    private static readonly string[] States = new[] { "normal", "hover", "click" };

    /// <summary>
    /// Erzeugt eine standardisierte Resource-ID für Control-States:
    /// Beispiel: MakeControlStateId("Default:Buttons", "btn_exit", "normal") -> "Default:Buttons:btn_exit_normal.png"
    /// (Konvention: theme:group:...:control_state.png)
    /// </summary>
    public static string MakeControlStateId(string themeKey, string controlId, string state, string ext = ".png") {
        if (string.IsNullOrWhiteSpace(themeKey))
            return $"{controlId}_{state}{ext}";
        return $"{themeKey}:{controlId}_{state}{ext}";
    }

    /// <summary>
    /// Lädt Bild- und Skia-Ressourcen für Control-States.
    /// Provider-first, dann Filesystem-Fallback (ResFolder). IDs im Format "theme:group:file.png" werden
    /// für den Filesystem-Fallback in Pfadsegmente aufgeteilt.
    /// </summary>
    public static void LoadControlStateResources(this IResourceProvider? provider, string themeKey, string id,
        Dictionary<string, Image> stateImages, Dictionary<string, SKBitmap> stateBitmaps) {

        foreach (var state in States) {
            var resourceId = MakeControlStateId(themeKey, id, state);

            // Provider versuchen (wenn vorhanden)
            if (provider != null) {
                try {
                    var sk = provider.TryGetSkiaBitmap(resourceId);
                    if (sk != null && !stateBitmaps.ContainsKey(state))
                        stateBitmaps[state] = sk;

                    using var stream = provider.OpenStream(resourceId);
                    if (stream != null && !stateImages.ContainsKey(state)) {
                        try {
                            using var img = Image.FromStream(stream);
                            stateImages[state] = new Bitmap(img); // klonen
                        }
                        catch {
                            // ignore image load failures from provider stream
                        }
                    }
                }
                catch {
                    // provider errors ignored -> fallback folgt
                }
            }

            // Filesystem-Fallback: konvertiere "theme:group:..." zu relativen Pfadsegmenten
            if (!stateBitmaps.ContainsKey(state) || !stateImages.ContainsKey(state)) {
                var rel = resourceId
                    .Replace(':', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var path = Path.Combine(Paths.ResFolder, rel);

                if (File.Exists(path)) {
                    if (!stateImages.ContainsKey(state)) {
                        try {
                            using var img = Image.FromFile(path);
                            stateImages[state] = new Bitmap(img);
                        }
                        catch {
                            // ignore
                        }
                    }

                    if (!stateBitmaps.ContainsKey(state)) {
                        try {
                            var sk = SKBitmap.Decode(path);
                            if (sk != null)
                                stateBitmaps[state] = sk;
                        }
                        catch {
                            // ignore
                        }
                    }
                }
            }
        }
    }
}
