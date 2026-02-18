// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.UI.Control;
using KUpdater.UI.Themes;
using KUpdater.Utility;
using Button = KUpdater.UI.Control.Button;
using Label = KUpdater.UI.Control.Label;

namespace KUpdater.UI.Markup;


public static class ControlFactory {
    public static IControl Create(ControlConfig cfg, WindowContext ctx) {
        Rectangle bounds = new Rectangle(
            cfg.Bounds!.X,
            cfg.Bounds.Y,
            cfg.Bounds.Width,
            cfg.Bounds.Height
        );

        Rectangle boundsFunc() {
            int x = cfg.Bounds.X;
            int y = cfg.Bounds.Y;

            if (x < 0)
                x = ctx.Backend.Width + x;

            if (y < 0)
                y = ctx.Backend.Height + y;

            return new Rectangle(x, y, cfg.Bounds.Width, cfg.Bounds.Height);
        }

        var font = CreateFont(cfg.Font);
        var color = MakeColor.FromHex(cfg.Color ?? "#FFFFFF");

        return cfg.Type switch {
            "Label" => new Label(
                cfg.Id,
                boundsFunc,
                cfg.Text ?? "",
                font,
                color
            ) { Layer = Enum.Parse<ControlLayer>(cfg.Layer) },

            "Button" => new Button(
                cfg.Id,
                boundsFunc,
                cfg.Text ?? "",
                font,
                color,
                cfg.SkinKey ?? "",
                ResolveClick(cfg.OnClick, ctx)
            ) { Layer = Enum.Parse<ControlLayer>(cfg.Layer) },

            _ => throw new Exception($"Unknown control type: {cfg.Type}")
        };
    }

    private static Font CreateFont(FontConfig? cfg) {
        if (cfg == null)
            return new Font("Arial", 14);

        var style = cfg.Style?.ToLower() switch
    {
        "italic" => FontStyle.Italic,
        "bold" => FontStyle.Bold,
        _ => FontStyle.Regular
    };

        return new Font(cfg.Name, cfg.Size, style);
    }

    private static Action? ResolveClick(string? name, WindowContext ctx) {
        return name switch {
            "closeWindow" => () => ctx.Backend.CloseWindow(),
            _ => null
        };
    }
}
