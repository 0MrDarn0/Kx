// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Platform;


/// <summary>
/// Fluent configuration object for tray icons and menu.
/// This class no longer creates a NotifyIcon; it only stores configuration.
/// </summary>
public sealed class TrayIcon : IDisposable {
    private string? _text;
    private Icon? _icon;
    private readonly Dictionary<string, Icon> _statusIcons = [];
    private TrayMenuBuilder? _menuBuilder;

    public TrayIcon Name(string name) { _text = name; return this; }

    public TrayIcon Icon(string filePath) {
        if (File.Exists(filePath)) {
            _icon?.Dispose();
            _icon = new Icon(filePath);
        }
        return this;
    }

    public TrayIcon Icon(System.Drawing.Icon icon) {
        _icon?.Dispose();
        _icon = (Icon)icon.Clone();
        return this;
    }

    public TrayIcon Menu(Action<TrayMenuBuilder> buildAction) {
        var builder = new TrayMenuBuilder();
        buildAction(builder);
        _menuBuilder = builder;
        return this;
    }

    public TrayIcon StatusIcons(Action<StatusIconBuilder> buildAction) {
        var builder = new StatusIconBuilder();
        buildAction(builder);
        foreach (var kv in builder.Build()) {
            // clone or store reference; ensure disposal later
            _statusIcons[kv.Key] = (Icon)kv.Value.Clone();
        }
        return this;
    }

    internal string? ConfiguredText => _text;
    internal Icon? ConfiguredIcon => _icon;
    internal IReadOnlyDictionary<string, Icon> BuildStatusIcons() => _statusIcons;
    internal ContextMenuStrip? BuildContextMenu() {
        if (_menuBuilder == null)
            return null;

        var menu = new ContextMenuStrip();

        static ToolStripItem CreateItem(TrayMenuBuilder.MenuEntry entry) {
            if (entry.IsSeparator)
                return new ToolStripSeparator();
            var menuItem = new ToolStripMenuItem(entry.Label);
            if (entry.Handler != null)
                menuItem.Click += entry.Handler;
            if (entry.SubItems != null && entry.SubItems.Count > 0) {
                foreach (var sub in entry.SubItems)
                    menuItem.DropDownItems.Add(CreateItem(sub));
            }
            return menuItem;
        }

        foreach (var entry in _menuBuilder.Build())
            menu.Items.Add(CreateItem(entry));

        return menu;
    }

    public void Dispose() {
        foreach (var icon in _statusIcons.Values)
            icon.Dispose();
        _statusIcons.Clear();
        _icon?.Dispose();
        _icon = null;
        // _menuBuilder is not disposable
    }
}

public class TrayMenuBuilder {
    internal class MenuEntry(string label, EventHandler? handler, bool isSeparator, List<TrayMenuBuilder.MenuEntry>? subItems = null) {
        public string Label { get; } = label;
        public EventHandler? Handler { get; } = handler;
        public bool IsSeparator { get; } = isSeparator;
        public List<MenuEntry>? SubItems { get; } = subItems;
    }

    internal readonly List<MenuEntry> _items = [];

    public TrayMenuBuilder Item(string label, EventHandler onClick) {
        _items.Add(new MenuEntry(label, onClick, false));
        return this;
    }

    public TrayMenuBuilder Item(string label, Action<TrayMenuBuilder> submenuBuilder) {
        var sub = new TrayMenuBuilder();
        submenuBuilder(sub);
        _items.Add(new MenuEntry(label, null, false, [.. sub._items]));
        return this;
    }

    public TrayMenuBuilder Separator() {
        _items.Add(new MenuEntry(string.Empty, null, true));
        return this;
    }

    public TrayMenuBuilder Exit(EventHandler onClick, string label = "Exit") {
        _items.Add(new MenuEntry(label, onClick, false));
        return this;
    }

    internal IReadOnlyList<MenuEntry> Build() => _items;
}

public class StatusIconBuilder {
    private readonly Dictionary<string, Icon> _icons = [];

    public StatusIconBuilder Item(string key, string filePath) {
        if (File.Exists(filePath))
            _icons[key] = new Icon(filePath);
        return this;
    }

    public StatusIconBuilder Item(string key, Icon icon) {
        _icons[key] = (Icon)icon.Clone();
        return this;
    }

    internal IReadOnlyDictionary<string, Icon> Build() => _icons;
}
