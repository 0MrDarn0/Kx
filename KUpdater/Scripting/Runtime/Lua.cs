// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Reflection;
using KUpdater.Core.Attributes;
using KUpdater.Extensions;
using KUpdater.UI.Control;
using KUpdater.Utility;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using SkiaSharp;

namespace KUpdater.Scripting.Runtime;

public abstract class Lua : IDisposable {
    protected readonly Script _script;
    public Script Script => _script;
    private bool _disposed;

    public Lua(string path) {
        string scriptPath = Paths.LuaScript(path);
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Lua script not found: {scriptPath}");

        _script = new Script();
        _script.Options.DebugPrint = s => Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Lua] >>> [{s}]");

        ConfigureModulePaths(Paths.LuaFolder);
        RegisterGlobals();

        _script.DoString(File.ReadAllText(scriptPath));

    }

    protected virtual void RegisterGlobals() {
        SetGlobal("__debug_globals", (Action)(() => {
            Debug.WriteLine("=== Lua Globals ===");
            foreach (var pair in _script.Globals.Pairs)
                Debug.WriteLine($"{pair.Key.ToPrintString()} : {pair.Value.Type}");

        }));
        SetGlobal("exe_directory", Paths.Base);
    }

    private static void ConfigureModulePaths(string luaRoot) {
        var loader = (ScriptLoaderBase)Script.DefaultOptions.ScriptLoader;

        // Alle Unterordner rekursiv einsammeln
        var dirs = Directory.GetDirectories(luaRoot, "*", SearchOption.AllDirectories);

        // Für jeden Ordner ein Pattern hinzufügen
        var paths = dirs.SelectMany(d => new[] {
            Path.Combine(d, "?.lua"),
            Path.Combine(d, "?", "?.lua"),
            Path.Combine(d, "?", "init.lua")
        }).ToList();

        // Root selbst auch berücksichtigen
        paths.Add(Path.Combine(luaRoot, "?.lua"));
        paths.Add(Path.Combine(luaRoot, "?", "?.lua"));
        paths.Add(Path.Combine(luaRoot, "?", "init.lua"));

        loader.ModulePaths = [.. paths];
    }

    protected internal void SetGlobal(string name, object value)
        => _script.Globals.Set(name, DynValue.FromObject(_script, value));

    protected LuaValue<T> GetGlobal<T>(string name)
         => new(_script.Globals.Get(name));

    protected DynValue InvokeClosure(DynValue func, params object[] args) {
        if (!func.IsFunction())
            return DynValue.Nil;

        try {
            var dynArgs = (args ?? Array.Empty<object>()).Select(a => DynValue.FromObject(_script, a)).ToArray();
            return _script.Call(func, dynArgs);
        }
        catch (ScriptRuntimeException srx) {
            Debug.WriteLine($"[Lua] runtime error invoking closure: {srx.DecoratedMessage ?? srx.Message}");
            Debug.WriteLine($"[Lua] raw stacktrace: {srx.StackTrace}");
            return DynValue.Nil;
        }
        catch (Exception ex) {
            Debug.WriteLine($"[Lua] error invoking closure: {ex}");
            return DynValue.Nil;
        }
    }


    public DynValue Invoke(string functionName, params object[] args) {
        try {
            var func = GetGlobal<DynValue>(functionName).Raw;
            return InvokeClosure(func, args);
        }
        catch (ScriptRuntimeException srx) {
            Debug.WriteLine($"[Lua] Runtime error invoking {functionName}: {srx.Message}");
            return DynValue.Nil;
        }
        catch (Exception ex) {
            Debug.WriteLine($"[Lua] Error invoking {functionName}: {ex.Message}");
            return DynValue.Nil;
        }
    }


    public DynValue Invoke(DynValue func, params object[] args)
       => InvokeClosure(func, args);


    public LuaValue<T> Invoke<T>(string functionName, params object[] args)
       => new(Invoke(functionName, args));


    public LuaValue<T> Invoke<T>(DynValue func, params object[] args)
       => new(Invoke(func, args));

    public static T SafeCall<T>(Func<T> action, T fallback = default!) {
        try { return action(); }
        catch (Exception ex) { Debug.WriteLine($"[SafeCall] {ex.Message}"); return fallback; }
    }

    public DynValue SafeInvokeDyn(DynValue func, params object[] args) {
        try { return Invoke(func, args); }
        catch (Exception ex) { Debug.WriteLine($"[Lua] SafeInvoke failed: {ex.Message}"); return DynValue.Nil; }
    }


    public Table GetTableOrEmpty(string name) {
        var val = GetGlobal<DynValue>(name);
        return val.AsTableOrNew(_script);
    }


    public DynValue GetValue(string path) {
        var parts = path.Split('.');
        DynValue node = GetGlobal<DynValue>(parts[0]);
        for (int i = 1; i < parts.Length; i++) {
            if (!node.IsTable())
                return DynValue.Nil;
            node = node.Table.Get(parts[i]);
        }
        return node;
    }


    public string? GetString(string path) {
        var val = new LuaValue<DynValue>(GetValue(path));
        return val.AsString();
    }


    public void DumpTable(string path) {
        var val = new LuaValue<DynValue>(GetValue(path));
        if (!val.IsTable()) {
            Debug.WriteLine($"[Lua] {path} is not a table.");
            return;
        }

        Debug.WriteLine($"[Lua] Dumping table: {path}");
        foreach (var pair in val.AsTable()!.Pairs)
            Debug.WriteLine($"  {pair.Key.ToPrintString()} = {pair.Value}");
    }


    public void ExposeToLua<T>(string? globalName = null, T? instance = default) {
        var type = typeof(T);
        globalName ??= type.Name;

        UserData.RegisterType<T>();

        // 1) Enums: expose static and individual values
        if (type.IsEnum) {
            SetGlobal(globalName, UserData.CreateStatic<T>());
            foreach (var name in Enum.GetNames(type)) {
                var value = Enum.Parse(type, name);
                SetGlobal(name, UserData.Create(value));
            }
            return;
        }

        // 2) Instance: expose the instance only (userdata)
        if (instance is not null) {
            SetGlobal(globalName, UserData.Create(instance));
            return;
        }

        // 3) Always expose statics
        SetGlobal(globalName, UserData.CreateStatic<T>());

        // Do not expose a constructor for types like Color/SKColor (we want Color.White, etc.)
        bool exposeConstructor =
            type != typeof(Color) &&
            type != typeof(SKColor) &&
            !type.IsAbstract &&
            type.GetConstructors().Length > 0;

        if (!exposeConstructor)
            return;

        // 4) Constructor dispatcher without noisy exceptions
        SetGlobal(globalName, DynValue.NewCallback((ctx, args) => {
            try {
                // Map MoonSharp DynValues to raw objects, but keep closures/tables intact for per-parameter matching.
                var rawArgs = args.GetArray().Select(a => a.MapDynValue()).ToArray();

                ConstructorInfo? chosen = null;
                object[]? finalArgs = null;

                foreach (var ctor in type.GetConstructors()) {

                    var parms = ctor.GetParameters();
                    int requiredCount = parms.Count(p => !p.HasDefaultValue);

                    if (rawArgs.Length < requiredCount || rawArgs.Length > parms.Length)
                        continue;

                    var tmp = new object?[parms.Length];
                    bool ok = true;

                    for (int i = 0; i < parms.Length; i++) {
                        var targetType = parms[i].ParameterType;

                        if (i < rawArgs.Length) {
                            var argVal = rawArgs[i];

                            if (!TryCoerce(argVal, targetType, out var coerced)) {
                                ok = false;
                                break;
                            }

                            tmp[i] = coerced;
                        } else {
                            if (parms[i].HasDefaultValue)
                                tmp[i] = parms[i].DefaultValue;
                            else {
                                ok = false;
                                break;
                            }
                        }
                    }

                    if (ok) {
                        chosen = ctor;
                        finalArgs = tmp!;
                        break;
                    }
                }

                if (chosen == null) {
                    Debug.WriteLine($"[Lua] No matching constructor for {type.Name} with {rawArgs.Length} args");
                    return DynValue.Nil;
                }

                var obj = chosen.Invoke(finalArgs!);
                return UserData.Create(obj);

            }
            catch (Exception ex) {
                Debug.WriteLine($"[Lua] Constructor dispatch error for {type.Name}: {ex.Message}");
                return DynValue.Nil;
            }
        }));

        // Local helper: targeted coercion without throwing exceptions
        bool TryCoerce(object? argVal, Type targetType, out object? result) {
            result = null;

            // Null handling
            if (argVal is null) {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    return false;
                result = null;
                return true;
            }

            var srcType = argVal.GetType();

            // Direct assignable
            if (targetType.IsAssignableFrom(srcType)) {
                result = argVal;
                return true;
            }

            // Lua Closure → Action
            if (targetType == typeof(Action) && argVal is Closure cb) {
                result = new Action(() => cb.Call());
                return true;
            }

            // Lua Closure → Func<Rectangle>
            if (targetType == typeof(Func<Rectangle>) && argVal is Closure boundsClosure) {
                result = new Func<Rectangle>(() => {
                    try {

                        var ret = boundsClosure.Call();
                        if (!ret.IsTable())
                            return Rectangle.Empty;

                        var t = ret.AsTable()!;
                        int x = (int)(t.Get("x").AsNumber() ?? 0);
                        int y = (int)(t.Get("y").AsNumber() ?? 0);
                        int w = (int)(t.Get("width").AsNumber() ?? 0);
                        int h = (int)(t.Get("height").AsNumber() ?? 0);

                        // Optional anchoring for negatives (keeps Lua simple)
                        var form = MainWindow.Instance;
                        if (form != null) {
                            if (x < 0)
                                x = form.Width + x;
                            if (y < 0)
                                y = form.Height + y;
                            if (w < 0)
                                w = form.Width + w;
                            // h negative rarely used; add if needed
                        }
                        return new Rectangle(x, y, w, h);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"[Lua] boundsClosure failed: {ex.Message}");
                        return Rectangle.Empty;
                    }
                });
                return true;
            }

            // Enum: string name
            if (targetType.IsEnum && argVal is string s &&
                Enum.TryParse(targetType, s, true, out var enumVal)) {
                result = enumVal;
                return true;
            }

            // Enum: numeric
            if (targetType.IsEnum && argVal is double dnum) {
                result = Enum.ToObject(targetType, (int)dnum);
                return true;
            }

            // Numeric coercions from MoonSharp's double
            if (argVal is double d) {
                if (targetType == typeof(int)) { result = (int)d; return true; }
                if (targetType == typeof(float)) { result = (float)d; return true; }
                if (targetType == typeof(long)) { result = (long)d; return true; }
                if (targetType == typeof(short)) { result = (short)d; return true; }
                if (targetType == typeof(byte)) { result = (byte)d; return true; }
                if (targetType == typeof(decimal)) { result = (decimal)d; return true; }
                if (targetType == typeof(double)) { result = d; return true; }
            }

            // String → numeric (rare, but safe)
            if (argVal is string str) {
                if (targetType == typeof(int) && int.TryParse(str, out var i)) { result = i; return true; }
                if (targetType == typeof(double) && double.TryParse(str, out var dd)) { result = dd; return true; }
                if (targetType == typeof(float) && float.TryParse(str, out var ff)) { result = ff; return true; }
                if (targetType == typeof(long) && long.TryParse(str, out var ll)) { result = ll; return true; }
                if (targetType == typeof(decimal) && decimal.TryParse(str, out var mm)) { result = mm; return true; }
            }

            // Table → Rectangle (if constructor directly expects Rectangle)
            if (targetType == typeof(Rectangle) && argVal is Table tbl) {
                int x = (int)(tbl.Get("x").AsNumber() ?? 0);
                int y = (int)(tbl.Get("y").AsNumber() ?? 0);
                int w = (int)(tbl.Get("width").AsNumber() ?? 0);
                int h = (int)(tbl.Get("height").AsNumber() ?? 0);

                var form = MainWindow.Instance;
                if (form != null) {
                    if (x < 0)
                        x = form.Width + x;
                    if (y < 0)
                        y = form.Height + y;
                    if (w < 0)
                        w = form.Width + w;
                }
                result = new Rectangle(x, y, w, h);
                return true;
            }

            // Last resort: try Convert.ChangeType only for simple primitives (avoid spamming exceptions)
            if (IsConvertiblePrimitive(srcType) && IsConvertiblePrimitive(targetType)) {
                try {
                    result = Convert.ChangeType(argVal, targetType);
                    return true;
                }
                catch {
                    // swallow; we'll return false
                }
            }
            return false;
        }

        static bool IsConvertiblePrimitive(Type t) {
            // Treat common primitives (including decimal, double) as convertible
            return t == typeof(bool) || t == typeof(byte) || t == typeof(short) ||
                   t == typeof(int) || t == typeof(long) || t == typeof(float) ||
                   t == typeof(double) || t == typeof(decimal) || t == typeof(char) ||
                   t == typeof(string);
        }
    }

    public void ExposeMarkedTypes() {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<ExposeToLuaAttribute>() != null)) {
            var attr = type.GetCustomAttribute<ExposeToLuaAttribute>()!;
            var globalName = attr.GlobalName ?? type.Name;

            var method = typeof(Lua).GetMethod(nameof(ExposeToLua))!;
            var generic = method.MakeGenericMethod(type);
            generic.Invoke(this, [globalName, null]);
        }
    }

    public void ExposeUIElements() {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(IControl).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)) {
            var method = typeof(Lua).GetMethod(nameof(ExposeToLua))!;
            var generic = method.MakeGenericMethod(type);
            generic.Invoke(this, [null, null]);
        }
    }


    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            // Managed Ressourcen freigeben

            // lobals leeren
            _script?.Globals.Clear();
        }

        // Unmanaged Ressourcen hier freigeben
        _disposed = true;
    }
}
