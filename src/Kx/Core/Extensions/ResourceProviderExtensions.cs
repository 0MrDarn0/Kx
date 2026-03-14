// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Utility;

using SkiaSharp;

namespace Kx.Core.Extensions;

public static class ResourceProviderExtensions {
    private static readonly string[] _states = ["normal", "hover", "click"];

    /// <summary>
    /// Erzeugt eine standardisierte GetResource-ID für Visual-States:
    /// Beispiel: MakeControlStateId("Default:Buttons", "btn_exit", "normal") -> "Default:Buttons:btn_exit_normal.png"
    /// (Konvention: skin:group:...:control_state.png)
    /// </summary>
    public static string MakeControlStateId(string skinId, string controlId, string state, string ext = ".png") {
        if (string.IsNullOrWhiteSpace(skinId))
            return $"{controlId}_{state}{ext}";
        return $"{skinId}:{controlId}_{state}{ext}";
    }

    /// <summary>
    /// Lädt Bild- und Skia-Ressourcen für Visual-States.
    /// Provider-first, dann Filesystem-Fallback (ResFolder). IDs im Format "theme:group:file.png" werden
    /// für den Filesystem-Fallback in Pfadsegmente aufgeteilt.
    /// </summary>
    public static void LoadControlStateResources(this IResourceProvider? provider, string skinKey, string id, Dictionary<string, SKBitmap> stateBitmaps) {

        foreach (var state in _states) {
            var resourceId = MakeControlStateId(skinKey, id, state);

            // Provider versuchen (wenn vorhanden)
            if (provider != null) {
                try {
                    var sk = provider.TryGetSkiaBitmap(resourceId);
                    if (sk != null && !stateBitmaps.ContainsKey(state))
                        stateBitmaps[state] = sk;
                }
                catch {
                    // provider errors ignored -> Fallback folgt
                }
            }

            // Filesystem-Fallback: konvertiere "skin:group:..." zu relativen Pfadsegmenten
            if (!stateBitmaps.ContainsKey(state)) {
                var rel = resourceId
                    .Replace(':', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var path = Path.Combine(Paths.ResFolder, rel);

                if (File.Exists(path)) {
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
