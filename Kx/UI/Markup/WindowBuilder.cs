// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.App;
using Kx.Abstractions.UI.Markup;
using Kx.Core.Configuration;
using Kx.UI.Themes;

namespace Kx.UI.Markup;

public static class WindowBuilder {
    public static void Build(WindowContext ctx, string yamlPath) {
        var config = ConfigLoader.Load<WindowConfig>(yamlPath);
        var frameResources = FrameResource.FromConfig(config.Frame, ctx.Resources, (ctx.Target.DeviceDpi / 96f));
        ctx.SetFrame(frameResources);

        //foreach (var c in config.Elements) {
        //    var control = ControlFactory.Create(c, ctx);
        //    switch (c.Layer) {
        //        case "Content":
        //        ctx.ContentRoot.Children.Add(control);
        //        break;

        //        default:
        //        ctx.UIElementManager.Add(control);
        //        break;
        //    }
        // }
    }
}
