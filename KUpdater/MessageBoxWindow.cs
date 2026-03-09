// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Abstractions.WindowHost;
using KUpdater.Core;
using KUpdater.UI.Rendering;
using KUpdater.UI.Themes;

namespace KUpdater;

public enum MessageBoxResult {
    None,
    Ok,
    Cancel,
    Yes,
    No
}

public class MessageBoxOptions {
    public bool AllowResizing { get; set; } = true;
    public bool Modal { get; set; } = true;
    public string[] Buttons { get; set; } = ["OK"]; // e.g. ["Yes","No"]
    public string DefaultButton { get; set; } = "OK";
}

public class MessageBoxWindow : IDisposable {
    private readonly IWindowHost _windowHost;
    private readonly WindowContext _ctx;
    private readonly WindowInteraction _interaction;
    private readonly TaskCompletionSource<MessageBoxResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly MessageBoxOptions _options;

    public MessageBoxWindow(IWindowHost windowHost, string title, string message, MessageBoxOptions? options = null) {
        _windowHost = windowHost;
        _options = options ?? new MessageBoxOptions();

        _ctx = new WindowContext(windowHost, windowHost, windowHost);

        var frameConfig = new FrameConfig();
        var frame = FrameResource.FromConfig(frameConfig, _ctx.Resources, (_ctx.Target.DeviceDpi / 96f));
        _ctx.SetFrame(frame);

        var renderer = new LayeredWindowRenderer(_ctx);
        _ctx.SetRenderer(renderer);

        _interaction = new WindowInteraction(_windowHost, _ctx, _options.AllowResizing);
        _windowHost.SetSize(670, 300);
    }


    public Task<MessageBoxResult> ShowAsync() {
        _windowHost.BeginInvoke((Action)(() => {
            if (_options.Modal && _windowHost is Form form) {
                Task.Run(() => {
                    var dr = form.ShowDialog();
                });
            } else {
                _windowHost.ShowWindow();
            }
        }));

        return _tcs.Task;
    }

    public void CloseWithResult(MessageBoxResult result) {
        _windowHost.BeginInvoke((Action)(() => {
            try {
                _windowHost.CloseWindow();
            }
            finally {
                _tcs.TrySetResult(result);
            }
        }));
    }

    public MessageBoxResult ShowDialog() {
        if (_windowHost is Form form) {
            var dr = form.ShowDialog();
            return _tcs.Task.IsCompleted ? _tcs.Task.Result : MessageBoxResult.None;
        }
        return ShowAsync().GetAwaiter().GetResult();
    }


    public void Show() {
        _windowHost.ShowWindow();
    }

    public void Close() {
        _windowHost.CloseWindow();
    }

    public void Dispose() {
        if (!_tcs.Task.IsCompleted) {
            _tcs.TrySetCanceled();
        }
        _ctx.Dispose();
    }

}
