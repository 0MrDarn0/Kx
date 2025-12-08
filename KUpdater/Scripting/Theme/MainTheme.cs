// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.UI;
using KUpdater.Scripting.Runtime;
using KUpdater.Scripting.Security;
using KUpdater.UI;
using KUpdater.Utility;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting.Theme;

public class MainTheme(Form form, ControlManager controlManager, UIState state, string lang, IResourceProvider resourceProvider)
    : ThemeBase("theme_loader.lua", form, controlManager, state, lang, resourceProvider) {

    protected override string GetThemeName() => "main_form";

    protected override void RegisterGlobals() {
        base.RegisterGlobals();
        LuaPolicy.Clear();
        LuaPolicy.Grant("Process.Start");
        LuaPolicy.Grant("Website.Open");
        LuaPathGuard.SetAllowedRoots(AppDomain.CurrentDomain.BaseDirectory);

        SetGlobal(LuaKeys.Theme.ThemeDir, Paths.LuaThemes.Replace("\\", "/"));
        SetGlobal(LuaKeys.UI.GetWindowSize, () => DynValue.NewTuple(
            DynValue.NewNumber(_form.Width),
            DynValue.NewNumber(_form.Height)
        ));
        SetGlobal(LuaKeys.Actions.ApplicationExit, (Action)(() => Application.Exit()));

        ExposeToLua("Controls", _controlManager);
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


    protected override ThemeBackground BuildBackground() {
        var bg = new ThemeTable(GetThemeTable("background"), Script);
        return new ThemeBackground {
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

    protected override ThemeLayout BuildLayout() {
        var layout = new ThemeTable(GetThemeTable("layout"), Script);
        return new ThemeLayout {
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
