// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core;
using KUpdater.Scripting.Skin;
using KUpdater.UI;
using KUpdater.UI.Interface;
using KUpdater.UI.Rendering;

public class MessageBoxWindow : IDisposable {
    private readonly IWindowBackend _backend;
    private readonly WindowContext _ctx;
    private readonly WindowInteraction _interaction;

    public MessageBoxWindow(IWindowBackend backend, string title, string message) {
        _backend = backend;

        _ctx = new WindowContext(backend, backend, backend);

        var skin = new MessageBoxSkin(_ctx, title, message);
        _ctx.SetSkin(skin);

        var renderer = new Renderer(_ctx);
        _ctx.SetRenderer(renderer);

        _interaction = new WindowInteraction(_backend, _ctx, false);
        _backend.SetSize(670, 300);
    }

    public void Show() {
        _backend.ShowWindow();
    }

    public void Close() {
        _backend.CloseWindow();
    }

    public void Dispose() {
        _ctx.Dispose();
    }
}
