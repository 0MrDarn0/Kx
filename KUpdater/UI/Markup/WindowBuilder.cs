// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Core.Configuration;
using KUpdater.UI.Themes;

namespace KUpdater.UI.Markup;

public static class WindowBuilder {
    public static void Build(WindowContext ctx, string yamlPath) {
        var config = ConfigLoader.Load<WindowConfig>(yamlPath);
        var frameResources = FrameResource.FromConfig(config.Frame, ctx.Resources, (ctx.Target.DeviceDpi / 96f));
        ctx.SetFrame(frameResources);

        foreach (var c in config.Controls) {
            var control = ControlFactory.Create(c, ctx);
            switch (c.Layer) {
                case "Content":
                ctx.ContentRoot.Children.Add(control);
                break;

                default:
                ctx.Controls.Add(control);
                break;
            }
        }
    }
}
