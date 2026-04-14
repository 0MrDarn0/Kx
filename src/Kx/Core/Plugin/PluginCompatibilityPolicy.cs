// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Plugin;

/// <summary>
/// Evaluates whether a plugin manifest is compatible with the current host API.
/// </summary>
public sealed class PluginCompatibilityPolicy {
    private readonly string _hostApiVersion;
    private readonly PluginDiagnostics _diagnostics;

    /// <summary>
    /// Initializes a new compatibility policy for a host API version.
    /// </summary>
    /// <param name="hostApiVersion">The host API version to validate against.</param>
    public PluginCompatibilityPolicy(string? hostApiVersion = null, PluginDiagnostics? diagnostics = null) {
        _hostApiVersion = string.IsNullOrWhiteSpace(hostApiVersion)
            ? HostInfo.ApiVersion
            : hostApiVersion;
        _diagnostics = diagnostics ?? new PluginDiagnostics();
    }

    /// <summary>
    /// Returns whether the specified plugin manifest is compatible with the host API version.
    /// </summary>
    /// <param name="manifest">The plugin manifest to validate.</param>
    public bool IsCompatible(PluginManifest manifest) {
        ArgumentNullException.ThrowIfNull(manifest);

        if (!Version.TryParse(manifest.ApiVersion, out var pluginApi) ||
            !Version.TryParse(_hostApiVersion, out var hostApi)) {
            _diagnostics.Trace("PluginLoader", $"Invalid API version format in plugin '{manifest.Name}'.");
            return false;
        }

        if (pluginApi.Major != hostApi.Major) {
            _diagnostics.Trace("PluginLoader", $"Plugin '{manifest.Name}' incompatible major API {pluginApi} vs host {hostApi}.");
            return false;
        }

        if (pluginApi.Minor > hostApi.Minor) {
            _diagnostics.Trace("PluginLoader", $"Plugin '{manifest.Name}' requires newer API {pluginApi} than host {hostApi}.");
            return false;
        }

        return true;
    }
}
