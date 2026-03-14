// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.UI.Platform;

public interface INotifyIconFactory { NotifyIcon Create(); }
public class DefaultNotifyIconFactory : INotifyIconFactory { public NotifyIcon Create() => new NotifyIcon(); }


public class TrayIconService : ITrayService {
    private readonly TrayIcon _config;
    private readonly INotifyIconFactory _factory;
    private readonly NotifyIcon _notifyIcon;
    private IReadOnlyDictionary<string, System.Drawing.Icon>? _statusIcons;
    private bool _disposed;

    public event EventHandler? Clicked;
    public event EventHandler? DoubleClicked;

    public TrayIconService(TrayIcon config, INotifyIconFactory? factory = null) {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _factory = factory ?? new DefaultNotifyIconFactory();
        _notifyIcon = _factory.Create();

        ApplyConfig();
        _notifyIcon.MouseClick += OnMouseClick;
        _notifyIcon.MouseDoubleClick += OnMouseDoubleClick;
        _notifyIcon.Visible = true;
    }

    private void ApplyConfig() {
        if (!string.IsNullOrEmpty(_config.ConfiguredText))
            _notifyIcon.Text = _config.ConfiguredText;

        if (_config.ConfiguredIcon != null)
            _notifyIcon.Icon = (System.Drawing.Icon)_config.ConfiguredIcon.Clone();

        _statusIcons = _config.BuildStatusIcons();

        var menu = _config.BuildContextMenu();
        if (menu != null)
            _notifyIcon.ContextMenuStrip = menu;
    }

    private void OnMouseClick(object? s, MouseEventArgs e) {
        if (e.Button == MouseButtons.Left)
            Clicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnMouseDoubleClick(object? s, MouseEventArgs e) {
        if (e.Button == MouseButtons.Left)
            DoubleClicked?.Invoke(this, EventArgs.Empty);
    }

    public void Show() => _notifyIcon.Visible = true;
    public void Hide() => _notifyIcon.Visible = false;

    public void SetStatus(string key) {
        if (_statusIcons != null && _statusIcons.TryGetValue(key, out var icon)) {
            _notifyIcon.Icon = (System.Drawing.Icon)icon.Clone();
            return;
        }

        if (_statusIcons != null && _statusIcons.TryGetValue("default", out var fallback)) {
            _notifyIcon.Icon = (System.Drawing.Icon)fallback.Clone();
            return;
        }

        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
    }

    public void ShowBalloon(string title, string text, int timeout = 2000) {
        var ctx = SynchronizationContext.Current;
        if (ctx == null) {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.ShowBalloonTip(timeout);
        } else {
            ctx.Post(_ => {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = text;
                _notifyIcon.ShowBalloonTip(timeout);
            }, null);
        }
    }

    public void Configure(Action<TrayIcon> configure) {
        configure?.Invoke(_config);
        // Re-apply configuration to runtime object
        ApplyConfig();
    }

    public void Dispose() {
        if (_disposed)
            return;
        _disposed = true;

        _notifyIcon.MouseClick -= OnMouseClick;
        _notifyIcon.MouseDoubleClick -= OnMouseDoubleClick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
