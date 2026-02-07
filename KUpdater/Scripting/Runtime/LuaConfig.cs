// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Reflection;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting.Runtime;

public class BaseConfig {
    public string Url { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string MainWindowSkin { get; set; } = "kalonline";
    //public NetworkConfig Network { get; set; } = new();
}

public class NetworkConfig {
    public ProxyConfig Proxy { get; set; } = new();
}

public class ProxyConfig {
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}


public class LuaConfig<T>(string scriptFile, string tableName) : Lua(scriptFile) where T : new() {
    private readonly string _tableName = tableName;

    public T Load() {
        var table = GetTableOrEmpty(_tableName);
        return (T)MapTableToObject(typeof(T), table)!;
    }

    public object? MapTableToObject(Type targetType, Table table) {
        var result = Activator.CreateInstance(targetType)!;

        foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!prop.CanWrite)
                continue;

            var key = prop.Name;
            var val = table.Get(key);

            if (val.IsNil())
                continue;

            bool set = false;
            try {
                if (prop.PropertyType == typeof(string)) {
                    prop.SetValue(result, val.CastToString() ?? string.Empty);
                    set = true;
                } else if (prop.PropertyType == typeof(int)) {
                    var n = val.CastToNumber();
                    prop.SetValue(result, (int)(n ?? 0));
                    set = true;
                } else if (prop.PropertyType == typeof(double)) {
                    var n = val.CastToNumber();
                    prop.SetValue(result, n ?? 0.0);
                    set = true;
                } else if (prop.PropertyType == typeof(bool)) {
                    prop.SetValue(result, val.CastToBool());
                    set = true;
                } else if (prop.PropertyType.IsEnum) {
                    if (val.Type == DataType.String && Enum.TryParse(prop.PropertyType, val.String, true, out var ev)) { prop.SetValue(result, ev); set = true; } else if (val.Type == DataType.Number) { prop.SetValue(result, Enum.ToObject(prop.PropertyType, (int)val.Number)); set = true; }
                } else if (val.Type == DataType.Table) {
                    var sub = MapTableToObject(prop.PropertyType, val.Table);
                    if (sub != null) { prop.SetValue(result, sub); set = true; }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"[LuaConfig] Failed to map {key} to {prop.Name}: {ex.Message}");
            }
            if (!set) {
                Debug.WriteLine($"[LuaConfig] No mapping applied for {prop.Name}, leaving default.");
            }
        }
        return result;
    }
}
