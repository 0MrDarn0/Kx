// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.UI;
using KUpdater.Extensions;
using KUpdater.Scripting.Runtime;
using KUpdater.UI;
using KUpdater.Utility;
using MoonSharp.Interpreter;
using SkiaSharp;

namespace KUpdater.Scripting.Skin;

public abstract class SkinBase : Lua, ISkin {
    protected readonly Window _targetWindow;
    protected readonly ControlManager _controlManager;
    protected readonly UIState _state;
    protected readonly IResourceProvider _resourceProvider;
    private SkinBackground? _cachedBackground;
    private SkinLayout? _cachedLayout;

    protected SkinBase(string skinScript, Window targetwindow, ControlManager controlManager, UIState state, string lang, IResourceProvider resourceProvider)
        : base(skinScript) {
        _targetWindow = targetwindow;
        _controlManager = controlManager;
        _state = state;
        _resourceProvider = resourceProvider;
        RegisterGlobals();
        LoadLanguage(lang);
        LoadSkin(GetName());
    }

    protected SKBitmap? GetSkiaBitmapFromProvider(string? id) {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        try {
            // Provider bietet TryGetSkiaBitmap; verwende diese (non-throwing)
            var sk = _resourceProvider.TryGetSkiaBitmap(id);
            return sk;
        }
        catch {
            return null;
        }
    }

    protected abstract string GetName();

    protected override void RegisterGlobals() {
        base.RegisterGlobals();
    }

    protected void LoadLanguage(string langCode) {
        var langPath = Paths.LuaLanguage(langCode);
        var fallbackPath = Paths.LuaDefaultLanguage;
        var langTable = Script.DoString(File.ReadAllText(langPath)).Table;
        var fallbackTable = Script.DoString(File.ReadAllText(fallbackPath)).Table;
        SetGlobal("L", langTable);
        SetGlobal("L_Fallback", fallbackTable);
        SetGlobal("T", (Func<string, string>)(key => {
            string? Lookup(Table table) {
                var node = DynValue.NewTable(table);
                foreach (var part in key.Split('.')) {
                    if (!node.IsTable())
                        return null;
                    node = node.Table.Get(part);
                }
                return node.AsString();
            }
            return Lookup(langTable) ?? Lookup(fallbackTable) ?? $"[MISSING:{key}]";
        }));
        Localization.Initialize(Script);
    }

    protected void LoadSkin(string skinName) {
        Invoke(LuaKeys.Skin.Load, skinName);
        var initFunc = new LuaValue<Closure>(Invoke(LuaKeys.Skin.Get).Table.Get("init"));
        if (initFunc.IsValid)
            Invoke(initFunc.Raw);
    }

    protected Table GetSkinTable(string key) {
        var skin = Invoke(LuaKeys.Skin.Get).Table;
        var table = new LuaValue<Table>(skin.Get(key));
        return table.Value ?? new Table(Script);
    }

    public void ApplyLastState() => UpdateLastState();
    public SkinBackground GetBackground() => _cachedBackground ??= BuildBackground();
    public SkinLayout GetLayout() => _cachedLayout ??= BuildLayout();

    protected abstract void UpdateLastState();
    protected abstract SkinBackground BuildBackground();
    protected abstract SkinLayout BuildLayout();
}
