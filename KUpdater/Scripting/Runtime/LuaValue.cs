// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Extensions;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting.Runtime;

public readonly struct LuaValue<T> {
    public DynValue Raw { get; }
    public T? Value { get; }
    public bool IsValid { get; }

    public LuaValue(DynValue raw) {
        Raw = raw;
        try {
            Value = raw.ToObject<T>();
            IsValid = Value is not null;
        }
        catch {
            Value = default;
            IsValid = false;
        }
    }

    public T GetOrDefault(T fallback) => IsValid ? Value! : fallback;
    public bool TryGet(out T? val) { val = Value; return IsValid; }

    public override string ToString()
        => IsValid ? Value?.ToString() ?? "null" : $"[Invalid LuaValue<{typeof(T).Name}>]";

    // 🔹 Forwarder zu LuaExtensions
    public bool IsTruthy() => Raw.IsTruthy();
    public bool IsFalsy() => Raw.IsFalsy();
    public bool IsTable() => Raw.IsTable();
    public bool IsString() => Raw.IsString();
    public bool IsNumber() => Raw.IsNumber();
    public bool IsFunction() => Raw.IsFunction();
    public bool IsUserData() => Raw.IsUserData();

    public string? AsString() => Raw.AsString();
    public double? AsNumber() => Raw.AsNumber();
    public Table? AsTable() => Raw.AsTable();
    public Closure? AsFunction() => Raw.AsFunction();
    public object? AsUserData() => Raw.AsUserData();

    public Color AsColor(Color fallback) => Raw.AsColor(fallback);
    public Table AsTableOrNew(Script script) => Raw.AsTable() ?? new Table(script);

    public static implicit operator DynValue(LuaValue<T> luaVal) => luaVal.Raw;  // Implizite Konvertierung zu DynValue
    public static implicit operator LuaValue<T>(DynValue raw) => new(raw);    // Implizite Konvertierung von DynValue zu LuaValue<T>

}
