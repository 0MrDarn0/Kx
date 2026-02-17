// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Drawing;
using KUpdater.Abstractions.Plugin;
using KUpdater.Abstractions.UI;
using KUpdater.Core;
using KUpdater.UI.Control;

namespace KUpdater.Plugins.DefaultUI;

public sealed class DefaultUiEngine : IUiEngine, IPlugin {
    public string Name => "DefaultUI";

    private WindowContext? _ctx;

    public void Initialize(IPluginContext context) {
        _ctx = (WindowContext)context.Services;

    }

    public void BuildMainWindow() {
        var titleLabel = new Label(
            id: "lb_title",
            boundsFunc: () => new Rectangle(35, 0, 200, 40),
            text: "KUpdater",
            font: new Font("Chiller", 40, FontStyle.Italic),
            color: Color.Orange
        );
        _ctx?.Controls.Add(titleLabel);

        var button = new Button(
            id: "btn_default",
            boundsFunc: () => new Rectangle(50, 50, 140, 40),
            text: "Update",
            font: new Font("Arial", 12),
            color: Color.White,
            skinKey: "KalOnline:Buttons",
            onClick: () =>
            {
                Debug.WriteLine("Update clicked!");
            }
        );

        _ctx?.Controls.Add(button);
    }

    public void Dispose() {
        Debug.WriteLine("[DefaultUiEngine] Disposed");
    }
}
