// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI;

/// <summary>
/// Provides a fluent API for creating and managing a system tray icon.
/// Supports tooltip text, custom icons, context menus, symbolic status icons,
/// balloon notifications, and standard events (click, double‑click, balloon events).
/// </summary>
public sealed class TrayIcon : IDisposable {
    private readonly NotifyIcon _notifyIcon;
    private readonly Dictionary<string, System.Drawing.Icon> _statusIcons = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIcon"/> class.
    /// By default, the tray icon is visible, uses <see cref="SystemIcons.Application"/>,
    /// and displays the tooltip text "Application".
    /// </summary>
    public TrayIcon() {
        _notifyIcon = new NotifyIcon {
            Visible = true,
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Application"
        };
    }

    // ---------------------------
    // Fluent configuration
    // ---------------------------

    /// <summary>
    /// Sets the tooltip text (application name) shown when hovering over the tray icon.
    /// </summary>
    /// <param name="name">The text to display in the tooltip.</param>
    /// <returns>The <see cref="TrayIcon"/> instance for fluent chaining.</returns>
    public TrayIcon Name(string name) {
        _notifyIcon.Text = name;
        return this;
    }

    /// <summary>
    /// Sets the tray icon from a file path pointing to an <c>.ico</c> file.
    /// If the file does not exist, the call is ignored.
    /// </summary>
    public TrayIcon Icon(string filePath) {
        if (File.Exists(filePath))
            _notifyIcon.Icon = new System.Drawing.Icon(filePath);
        return this;
    }

    /// <summary>
    /// Sets the tray icon from an existing <see cref="System.Drawing.Icon"/> instance.
    /// </summary>
    public TrayIcon Icon(System.Drawing.Icon icon) {
        _notifyIcon.Icon = icon;
        return this;
    }

    /// <summary>
    /// Configures the tray context menu using a fluent <see cref="TrayMenuBuilder"/>.
    /// The builder allows adding items, separators, and a standard Exit entry.
    /// </summary>
    public TrayIcon Menu(Action<TrayMenuBuilder> buildAction) {
        var builder = new TrayMenuBuilder();
        buildAction(builder);

        var menu = new ContextMenuStrip();
        foreach (var item in builder.Build()) {
            if (item.IsSeparator) {
                menu.Items.Add(new ToolStripSeparator());
            } else {
                var menuItem = new ToolStripMenuItem(item.Label);
                if (item.Handler != null) {
                    menuItem.Click += item.Handler;
                }
                menu.Items.Add(menuItem);
            }
        }

        _notifyIcon.ContextMenuStrip = menu;
        return this;
    }

    /// <summary>
    /// Configures symbolic status icons using a fluent <see cref="StatusIconBuilder"/>.
    /// Each status is identified by a string key (e.g. "default", "busy", "error").
    /// </summary>
    public TrayIcon StatusIcons(Action<StatusIconBuilder> buildAction) {
        var builder = new StatusIconBuilder();
        buildAction(builder);

        foreach (var kv in builder.Build())
            _statusIcons[kv.Key] = kv.Value;

        return this;
    }

    // ---------------------------
    // Events
    // ---------------------------

    /// <summary>
    /// Raised when the tray icon is left‑clicked once.
    /// Right‑clicks are ignored (reserved for context menu).
    /// </summary>
    public TrayIcon OnClick(EventHandler handler) {
        _notifyIcon.MouseClick += (s, e) => {
            if (e.Button == MouseButtons.Left)
                handler(s, e);
        };
        return this;
    }

    /// <summary>
    /// Raised when the tray icon is double‑clicked with the left mouse button.
    /// </summary>
    public TrayIcon OnDoubleClick(EventHandler handler) {
        _notifyIcon.MouseDoubleClick += (s, e) => {
            if (e.Button == MouseButtons.Left)
                handler(s, e);
        };
        return this;
    }

    /// <summary>
    /// Raised when the user clicks on a balloon notification shown by <see cref="ShowBalloon"/>.
    /// </summary>
    public TrayIcon OnBalloonClick(EventHandler handler) {
        _notifyIcon.BalloonTipClicked += handler;
        return this;
    }

    /// <summary>
    /// Raised when a balloon notification is dismissed,
    /// either by user action (close button) or automatically after timeout.
    /// </summary>
    public TrayIcon OnBalloonClosed(EventHandler handler) {
        _notifyIcon.BalloonTipClosed += handler;
        return this;
    }


    // ---------------------------
    // Runtime actions
    // ---------------------------

    /// <summary>
    /// Switches the tray icon to a previously registered status.
    /// </summary>
    /// <param name="key">The symbolic key of the status (e.g. "updating").</param>
    /// <remarks>
    /// Lookup order:
    /// <list type="number">
    ///   <item><description>Use the icon registered for <paramref name="key"/> if available.</description></item>
    ///   <item><description>Otherwise fall back to the icon registered as "default".</description></item>
    ///   <item><description>If neither is found, fall back to <see cref="SystemIcons.Application"/>.</description></item>
    /// </list>
    /// This guarantees that the tray icon is never left without an icon.
    /// </remarks>
    public void SetStatus(string key) {
        if (_statusIcons.TryGetValue(key, out var icon)) {
            _notifyIcon.Icon = icon;
        } else if (_statusIcons.TryGetValue("default", out var fallback)) {
            _notifyIcon.Icon = fallback;
        } else {
            // Fallback auf SystemIcons.Application
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }
    }


    /// <summary>
    /// Shows a balloon notification above the tray icon.
    /// </summary>
    /// <param name="title">Title text of the balloon.</param>
    /// <param name="text">Body text of the balloon.</param>
    /// <param name="timeout">Display duration in milliseconds (default: 2000).</param>
    public void ShowBalloon(string title, string text, int timeout = 2000) {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(timeout);
    }

    /// <summary>
    /// Hides the tray icon without disposing it. Can be shown again with <see cref="Show"/>.
    /// </summary>
    public void Hide() => _notifyIcon.Visible = false;

    /// <summary>
    /// Shows the tray icon again if it was hidden with <see cref="Hide"/>.
    /// </summary>
    public void Show() => _notifyIcon.Visible = true;

    /// <summary>
    /// Disposes the tray icon and releases all resources.
    /// The icon is automatically hidden before disposal.
    /// </summary>
    public void Dispose() {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}

/// <summary>
/// Fluent builder for tray context menus.
/// Provides methods to add items, separators, and a standard Exit entry.
/// </summary>
public class TrayMenuBuilder {
    private readonly List<(string Label, EventHandler? Handler, bool IsSeparator)> _items = [];

    /// <summary>
    /// Adds a clickable menu item with a direct event handler.
    /// </summary>
    public TrayMenuBuilder Item(string label, EventHandler onClick) {
        _items.Add((label, onClick, false));
        return this;
    }

    /// <summary>
    /// Adds a visual separator line to the menu.
    /// </summary>
    public TrayMenuBuilder Separator() {
        _items.Add(("", null, true));
        return this;
    }

    /// <summary>
    /// Adds a standard "Exit" menu item with a click handler.
    /// </summary>
    /// <param name="onClick">Handler invoked when Exit is clicked.</param>
    /// <param name="label">Optional label text (default: "Exit").</param>
    public TrayMenuBuilder Exit(EventHandler onClick, string label = "Exit") {
        _items.Add((label, onClick, false));
        return this;
    }

    internal IReadOnlyList<(string Label, EventHandler? Handler, bool IsSeparator)> Build()
        => _items;
}

/// <summary>
/// Fluent builder for symbolic status icons.
/// Allows registering custom states (keys) with associated icons.
/// </summary>
/// <remarks>
/// It is recommended to always register a "default" state,
/// which will be used as Fallback if no other state matches.
/// </remarks>
public class StatusIconBuilder {
    private readonly Dictionary<string, System.Drawing.Icon> _icons = [];

    /// <summary>
    /// Registers a status icon from a file path pointing to an <c>.ico</c> file.
    /// If the file does not exist, the call is ignored.
    /// </summary>
    public StatusIconBuilder Item(string key, string filePath) {
        if (File.Exists(filePath))
            _icons[key] = new System.Drawing.Icon(filePath);
        return this;
    }

    /// <summary>
    /// Registers a status icon from an existing <see cref="System.Drawing.Icon"/> instance.
    /// </summary>
    public StatusIconBuilder Item(string key, System.Drawing.Icon icon) {
        _icons[key] = icon;
        return this;
    }

    internal IReadOnlyDictionary<string, System.Drawing.Icon> Build() => _icons;
}
