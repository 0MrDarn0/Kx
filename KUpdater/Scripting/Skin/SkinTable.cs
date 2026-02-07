// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Extensions;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.Scripting.Skin;

public readonly struct SkinTable(Table table, Script script) {
    private readonly Table _table = table;
    private readonly Script _script = script;

    public string GetString(string key, string fallback = "")
        => _table.Get(key).AsString() ?? fallback;

    public int GetInt(string key, int fallback = 0)
        => (int)(_table.Get(key).AsNumber() ?? fallback);

    public double GetDouble(string key, double fallback = 0.0)
        => _table.Get(key).AsNumber() ?? fallback;

    public Color GetColor(string key, Color fallback)
        => _table.Get(key).AsColor(fallback);

    public Rectangle GetBounds(string key, Rectangle fallback)
        => _table.Get(key).As(fallback);

    public SKBitmap GetBitmap(string key, string resourceDir) {
        string? file = _table.Get(key).AsString();
        if (string.IsNullOrWhiteSpace(file))
            return new SKBitmap(1, 1);

        string path = Path.Combine(resourceDir, file);
        if (!File.Exists(path))
            return new SKBitmap(1, 1);

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var img = Image.FromStream(fs);
        return img.ToSKBitmap();
    }

    public DynValue Raw(string key) => _table.Get(key);
}
