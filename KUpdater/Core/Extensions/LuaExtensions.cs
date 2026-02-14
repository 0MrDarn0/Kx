// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using MoonSharp.Interpreter;

namespace KUpdater.Core.Extensions;

public static class LuaExtensions {

    // 🔹 Typensicheres Casten eines DynValue zu T
    public static T? As<T>(this DynValue val) {
        if (val.AsUserData() is T typed)
            return typed;
        try {
            return val.ToObject<T>();
        }
        catch {
            return default;
        }
    }

    // 🔹 Typensicheres Casten eines DynValue zu T mit Fallback value
    public static T As<T>(this DynValue val, T fallback) => val.As<T>() ?? fallback;

    public static bool IsTruthy(this DynValue val) => !val.IsNil() && !(val.Type == DataType.Boolean && val.Boolean == false);
    public static bool IsFalsy(this DynValue val) => !val.IsTruthy();

    public static bool IsNil(this DynValue val) => val.Type == DataType.Nil || val.Type == DataType.Void;
    public static bool IsTable(this DynValue val) => val.Type == DataType.Table;
    public static bool IsString(this DynValue val) => val.Type == DataType.String;
    public static bool IsNumber(this DynValue val) => val.Type == DataType.Number;
    public static bool IsFunction(this DynValue val) => val.Type == DataType.Function;
    public static bool IsUserData(this DynValue val) => val.Type == DataType.UserData;

    public static string? AsString(this DynValue val)
        => val.IsString() ? val.String : null;

    public static double? AsNumber(this DynValue val)
        => val.IsNumber() ? val.Number : null;

    public static Table? AsTable(this DynValue val)
        => val.IsTable() ? val.Table : null;

    public static Closure? AsFunction(this DynValue val)
        => val.IsFunction() ? val.Function : null;

    public static object? AsUserData(this DynValue val)
        => val.IsUserData() ? val.UserData.Object : null;

    public static Color AsColor(this DynValue val, Color fallback) {
        try {
            if (val.IsString()) {
                var s = val.AsString()!;
                if (s.StartsWith('#'))
                    return ColorTranslator.FromHtml(s); // unterstützt #RRGGBB und #RRGGBBAA
            }

            if (val.AsUserData() is Color c)
                return c;

            if (val.IsTable()) {
                var t = val.AsTable()!;
                int r = Clamp((int)(t.Get("r").AsNumber() ?? 0));
                int g = Clamp((int)(t.Get("g").AsNumber() ?? 0));
                int b = Clamp((int)(t.Get("b").AsNumber() ?? 0));
                int a = Clamp((int)(t.Get("a").AsNumber() ?? 255));
                return Color.FromArgb(a, r, g, b);
            }
        }
        catch { }

        return fallback;
    }
    private static int Clamp(int value) => Math.Max(0, Math.Min(255, value));

    public static object? MapDynValue(this DynValue val) {
        if (val.IsTable())
            return val.AsTable();
        if (val.IsFunction())
            return val.AsFunction();
        if (val.IsUserData())
            return val.AsUserData();
        return val.ToObject();
    }

    public static Rectangle ToRectangle(this Table t) {
        int x = (int)(t.Get("x").CastToNumber() ?? 0);
        int y = (int)(t.Get("y").CastToNumber() ?? 0);
        int w = (int)(t.Get("width").CastToNumber() ?? 0);
        int h = (int)(t.Get("height").CastToNumber() ?? 0);

        return new Rectangle(x, y, w, h);
    }

    public static Func<Rectangle> ToBoundsFunc(this Table t) => () => t.ToRectangle();

}
