// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using System.Runtime.Loader;

namespace Kx.Core.Plugin;

public sealed class PluginLoadContext(string mainAssemblyPath) : AssemblyLoadContext(isCollectible: true) {
    private readonly AssemblyDependencyResolver _resolver = new(mainAssemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName) {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
