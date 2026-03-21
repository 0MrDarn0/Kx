// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Plugin;

/// <summary>
/// Resolves dependency-respecting plugin load order from discovered manifests.
/// </summary>
public sealed class PluginDependencyResolver {
    /// <summary>
    /// Resolves load order for the specified plugin manifests.
    /// </summary>
    /// <param name="plugins">The discovered plugin manifests keyed by plugin name.</param>
    public IReadOnlyList<string> ResolveLoadOrder(IReadOnlyDictionary<string, PluginManifest> plugins) {
        ArgumentNullException.ThrowIfNull(plugins);

        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(string name) {
            if (visited.Contains(name))
                return;

            if (!plugins.ContainsKey(name))
                throw new InvalidOperationException($"Unknown plugin '{name}' in dependency graph.");

            if (visiting.Contains(name))
                throw new InvalidOperationException($"Cyclic dependency detected at '{name}'.");

            visiting.Add(name);

            foreach (var dependency in plugins[name].Dependencies) {
                if (!plugins.ContainsKey(dependency))
                    throw new InvalidOperationException($"Missing dependency '{dependency}' for plugin '{name}'.");

                Visit(dependency);
            }

            visiting.Remove(name);
            visited.Add(name);
            result.Add(name);
        }

        foreach (var name in plugins.Keys)
            Visit(name);

        return result;
    }
}
