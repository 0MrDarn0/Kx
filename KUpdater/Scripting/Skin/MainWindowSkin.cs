// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.Event;
using KUpdater.Core.UI;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Security;
using KUpdater.UI;
using KUpdater.Utility;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting.Skin;

public class MainWindowSkin(Window window, ControlManager controlManager, IEventManager eventManager, UIState state, string lang, string skinName, IResourceProvider resourceProvider)
    : SkinBase("skin_loader.lua", window, controlManager, eventManager, state, lang, skinName, resourceProvider) {

    protected override string GetName() => "main_window_skin";

    protected override void RegisterGlobals() {
        base.RegisterGlobals();
        LuaPolicy.Clear();
        LuaPolicy.Grant("Process.Start");
        LuaPolicy.Grant("Website.Open");
        LuaPathGuard.SetAllowedRoots(AppDomain.CurrentDomain.BaseDirectory);

        SetGlobal(LuaKeys.Skin.Dir, Paths.LuaSkins.Replace("\\", "/"));
        SetGlobal(LuaKeys.UI.GetWindowSize, () => DynValue.NewTuple(
            DynValue.NewNumber(_targetWindow.Width),
            DynValue.NewNumber(_targetWindow.Height)
        ));
        SetGlobal(LuaKeys.Actions.ApplicationExit, (Action)(() => Application.Exit()));

        ExposeToLua("Controls", _controlManager);
        ExposeToLua("EventManager", _eventManager);
        ExposeToLua("UIState", _state);
        ExposeToLua<Font>();
        ExposeToLua<Color>();
        ExposeMarkedTypes();

        //SetGlobal("update_status", (Action<string>)(text => _controlManager.Update<UI.Control.Label>("lb_update_status", l => l.Text = text)));
        //SetGlobal("update_download_progress", (Action<double>)(percent => _controlManager.Update<UI.Control.ProgressBar>("pb_update_progress", b => b.Progress = (float)Math.Clamp(percent, 0.0, 1.0))));
        //SetGlobal("update_label", UIBindings.UpdateLabel(_controlManager));
        //SetGlobal("update_progress", UIBindings.UpdateProgress(_controlManager));
    }

    protected override void UpdateLastState() {
        _controlManager.TryUpdate<UI.Control.Label>("lb_update_status", l => l.Text = _state.Status);
        _controlManager.TryUpdate<UI.Control.ProgressBar>("pb_update_progress", b => b.Progress = (float)_state.Progress);
        _controlManager.TryUpdate<UI.Control.ProgressBar>("pb_update_progress", b => b.Visible = _state.ProgressVisible);
        _controlManager.TryUpdate<UI.Control.TextBox>("tb_changelog", tb => tb.Text = _state.Changelog);
        _controlManager.TryUpdate<UI.Control.Button>("btn_start", btn => btn.Visible = _state.StartButtonVisible);
    }


    protected override SkinBackground BuildBackground() {
        var bg = new SkinTable(GetSkinTable("background"), Script);
        return new SkinBackground {
            TopLeft = GetSkiaBitmapFromProvider(bg.GetString("top_left")),
            TopCenter = GetSkiaBitmapFromProvider(bg.GetString("top_center")),
            TopRight = GetSkiaBitmapFromProvider(bg.GetString("top_right")),
            RightCenter = GetSkiaBitmapFromProvider(bg.GetString("right_center")),
            BottomRight = GetSkiaBitmapFromProvider(bg.GetString("bottom_right")),
            BottomCenter = GetSkiaBitmapFromProvider(bg.GetString("bottom_center")),
            BottomLeft = GetSkiaBitmapFromProvider(bg.GetString("bottom_left")),
            LeftCenter = GetSkiaBitmapFromProvider(bg.GetString("left_center")),
            FillColor = bg.GetColor("fill_color", Color.Black)
        };
    }

    protected override SkinLayout BuildLayout() {
        var layout = new SkinTable(GetSkinTable("layout"), Script);
        return new SkinLayout {
            TopWidthOffset = layout.GetInt("top_width_offset"),
            BottomWidthOffset = layout.GetInt("bottom_width_offset"),
            LeftHeightOffset = layout.GetInt("left_height_offset"),
            RightHeightOffset = layout.GetInt("right_height_offset"),
            FillPosOffset = layout.GetInt("fill_pos_offset"),
            FillWidthOffset = layout.GetInt("fill_width_offset"),
            FillHeightOffset = layout.GetInt("fill_height_offset")
        };
    }
}
