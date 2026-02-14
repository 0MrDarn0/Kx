// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using System.Runtime.Loader;

namespace KUpdater.Core.Plugin;

public sealed class PluginLoadContext : AssemblyLoadContext {
    private readonly AssemblyDependencyResolver _resolver;

    public string PluginPath { get; }

    public PluginLoadContext(string pluginPath)
        : base(isCollectible: true) {
        PluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName) {
        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
            return LoadFromAssemblyPath(path);

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
        string? path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (path != null)
            return LoadUnmanagedDllFromPath(path);

        return IntPtr.Zero;
    }
}
