// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Platform {
    /// <summary>
    /// Fluent configuration object for tray icons and menu.
    /// This class no longer creates a NotifyIcon; it only stores configuration.
    /// </summary>
    public sealed class TrayIcon : IDisposable {
        private string? _text;
        private System.Drawing.Icon? _icon;
        private readonly Dictionary<string, System.Drawing.Icon> _statusIcons = new();
        private TrayMenuBuilder? _menuBuilder;

        public TrayIcon Name(string name) { _text = name; return this; }

        public TrayIcon Icon(string filePath) {
            if (File.Exists(filePath)) {
                _icon?.Dispose();
                _icon = new System.Drawing.Icon(filePath);
            }
            return this;
        }

        public TrayIcon Icon(System.Drawing.Icon icon) {
            _icon?.Dispose();
            _icon = (System.Drawing.Icon)icon.Clone();
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
                _statusIcons[kv.Key] = (System.Drawing.Icon)kv.Value.Clone();
            }
            return this;
        }

        internal string? ConfiguredText => _text;
        internal System.Drawing.Icon? ConfiguredIcon => _icon;
        internal IReadOnlyDictionary<string, System.Drawing.Icon> BuildStatusIcons() => _statusIcons;
        internal ContextMenuStrip? BuildContextMenu() {
            if (_menuBuilder == null)
                return null;

            var menu = new ContextMenuStrip();

            ToolStripItem CreateItem(TrayMenuBuilder.MenuEntry entry) {
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
        internal class MenuEntry {
            public string Label { get; }
            public EventHandler? Handler { get; }
            public bool IsSeparator { get; }
            public List<MenuEntry>? SubItems { get; }

            public MenuEntry(string label, EventHandler? handler, bool isSeparator, List<MenuEntry>? subItems = null) {
                Label = label;
                Handler = handler;
                IsSeparator = isSeparator;
                SubItems = subItems;
            }
        }

        internal readonly List<MenuEntry> _items = new();

        public TrayMenuBuilder Item(string label, EventHandler onClick) {
            _items.Add(new MenuEntry(label, onClick, false));
            return this;
        }

        public TrayMenuBuilder Item(string label, Action<TrayMenuBuilder> submenuBuilder) {
            var sub = new TrayMenuBuilder();
            submenuBuilder(sub);
            _items.Add(new MenuEntry(label, null, false, new List<MenuEntry>(sub._items)));
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
        private readonly Dictionary<string, System.Drawing.Icon> _icons = new();

        public StatusIconBuilder Item(string key, string filePath) {
            if (File.Exists(filePath))
                _icons[key] = new System.Drawing.Icon(filePath);
            return this;
        }

        public StatusIconBuilder Item(string key, System.Drawing.Icon icon) {
            _icons[key] = (System.Drawing.Icon)icon.Clone();
            return this;
        }

        internal IReadOnlyDictionary<string, System.Drawing.Icon> Build() => _icons;
    }
}
