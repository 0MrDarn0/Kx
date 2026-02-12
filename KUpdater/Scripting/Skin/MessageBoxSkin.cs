// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Scripting.Runtime;
using KUpdater.Utility;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting.Skin;

public class MessageBoxSkin(WindowContext ctx, string title, string message)
    : SkinBase("skin_loader.lua", ctx.Config.Language, ctx.Config.MessageBoxSkin, ctx.Resources) {

    protected override string GetName() => "MessageBoxSkin";

    protected override void RegisterGlobals() {
        base.RegisterGlobals();
        SetGlobal(LuaKeys.Skin.Dir, Paths.LuaSkins.Replace("\\", "/"));
        SetGlobal(LuaKeys.UI.GetWindowSize, () => DynValue.NewTuple(
            DynValue.NewNumber(ctx.Target.Width),
            DynValue.NewNumber(ctx.Target.Height)
        ));

        SetGlobal("msg_title", title);
        SetGlobal("msg_text", message);

        SetGlobal("close_window", (Action)(() => {
            ctx.UiThread.BeginInvoke(new Action(() => {
                ctx.Backend?.CloseWindow();
            }));
        }));


        ctx.Events.SetSkin(this);

        ExposeToLua("Controls", ctx.Controls);
        ExposeToLua("Events", ctx.Events);
        ExposeToLua<Font>();
        ExposeToLua<Color>();
        ExposeMarkedTypes();
    }

    protected override void UpdateLastState() {
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
            FillBitmap = GetSkiaBitmapFromProvider(bg.GetString("fill_bitmap")),
            FillColor = bg.GetColor("fill_color", Color.Black),
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
