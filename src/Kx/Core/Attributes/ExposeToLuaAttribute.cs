// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
public sealed class ExposeToLuaAttribute(string? globalName = null) : Attribute {
    public string? GlobalName { get; } = globalName;
}
