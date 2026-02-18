// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Backend.BackendAbstractions;
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
    private readonly IWindowBackend _backend;
    private readonly WindowContext _ctx;
    private readonly WindowInteraction _interaction;
    private readonly TaskCompletionSource<MessageBoxResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly MessageBoxOptions _options;

    public MessageBoxWindow(IWindowBackend backend, string title, string message, MessageBoxOptions? options = null) {
        _backend = backend;
        _options = options ?? new MessageBoxOptions();

        _ctx = new WindowContext(backend, backend, backend);

        var frameConfig = new FrameConfig();
        var frame = FrameLoader.Load(frameConfig, _ctx.Resources);
        _ctx.SetFrame(frame);

        var renderer = new LayeredWindowRenderer(_ctx);
        _ctx.SetRenderer(renderer);

        _interaction = new WindowInteraction(_backend, _ctx, _options.AllowResizing);
        _backend.SetSize(670, 300);
    }


    public Task<MessageBoxResult> ShowAsync() {
        _backend.BeginInvoke((Action)(() => {
            if (_options.Modal && _backend is Form form) {
                Task.Run(() => {
                    var dr = form.ShowDialog();
                });
            } else {
                _backend.ShowWindow();
            }
        }));

        return _tcs.Task;
    }

    public void CloseWithResult(MessageBoxResult result) {
        _backend.BeginInvoke((Action)(() => {
            try {
                _backend.CloseWindow();
            }
            finally {
                _tcs.TrySetResult(result);
            }
        }));
    }

    public MessageBoxResult ShowDialog() {
        if (_backend is Form form) {
            var dr = form.ShowDialog();
            return _tcs.Task.IsCompleted ? _tcs.Task.Result : MessageBoxResult.None;
        }
        return ShowAsync().GetAwaiter().GetResult();
    }


    public void Show() {
        _backend.ShowWindow();
    }

    public void Close() {
        _backend.CloseWindow();
    }

    public void Dispose() {
        if (!_tcs.Task.IsCompleted) {
            _tcs.TrySetCanceled();
        }
        _ctx.Dispose();
    }

}
