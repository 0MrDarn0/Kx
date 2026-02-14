// Copyright (c) 2026 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Drawing;
using KUpdater.Abstractions.Plugin;
using KUpdater.Abstractions.UI;
using KUpdater.Core;
using KUpdater.UI.Control;

namespace KUpdater.UI.CSharp;

public class CSharpUiEngine : IUiEngine {
    private IPluginContext _context = null!;

    public string Name => "CSharp";

    public void Initialize(IPluginContext context) {
        _context = context;
    }

    public void BuildUi() {
        Debug.WriteLine("CSharpUiEngine void BuildUi()");
        var ctx = (WindowContext)_context;

        var titleLabel = new Label(
            id: "lb_title",
            boundsFunc: () => new Rectangle(35, 0, 200, 40),
            text: "KUpdater",
            font: new Font("Chiller", 40, FontStyle.Italic),
            color: Color.Orange
        );
        ctx.Controls.Add(titleLabel);

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

        ctx.Controls.Add(button);
    }
}
