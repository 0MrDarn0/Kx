// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

namespace Kx.UI.Rendering;

// Overlay failures must not take down the render pipeline.

internal sealed class RenderOverlayRegistry : IDisposable {
    private readonly Dictionary<string, IRenderOverlay> _overlays = new(StringComparer.Ordinal);
    private readonly HashSet<string> _enabledOverlayIds = new(StringComparer.Ordinal);

    public void Register(IRenderOverlay overlay) {
        ArgumentNullException.ThrowIfNull(overlay);

        if (_overlays.TryGetValue(overlay.Id, out IRenderOverlay? existing) && existing is IDisposable disposableExisting)
            disposableExisting.Dispose();

        _overlays[overlay.Id] = overlay;
    }

    public bool Toggle(string overlayId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayId);

        if (!_overlays.ContainsKey(overlayId))
            throw new InvalidOperationException($"Overlay '{overlayId}' is not registered.");

        if (_enabledOverlayIds.Contains(overlayId)) {
            _enabledOverlayIds.Remove(overlayId);
            return false;
        }

        _enabledOverlayIds.Add(overlayId);
        return true;
    }

    public void DrawOverlays(RenderOverlayContext context) {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IRenderOverlay overlay in _overlays.Values) {
            if (!_enabledOverlayIds.Contains(overlay.Id))
                continue;

            try {
                overlay.Draw(context);
            }
            catch (Exception ex) {
                Debug.WriteLine($"Overlay render error ({overlay.Id}): {ex}");
            }
        }
    }

    public void Dispose() {
        foreach (IRenderOverlay overlay in _overlays.Values) {
            if (overlay is IDisposable disposableOverlay)
                disposableOverlay.Dispose();
        }

        _enabledOverlayIds.Clear();
        _overlays.Clear();
    }
}
