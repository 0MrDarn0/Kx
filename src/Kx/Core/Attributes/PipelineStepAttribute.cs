// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PipelineStepAttribute(int order, string? name = null) : Attribute {
    public int Order { get; } = order;
    public string? Name { get; } = name;
}
