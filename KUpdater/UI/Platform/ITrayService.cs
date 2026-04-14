// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI.Platform;

/// <summary>
/// Abstraktion für Systemtray‑Funktionen.
/// </summary>
public interface ITrayService : IDisposable {
    /// <summary>Zeigt das Tray‑Icon an.</summary>
    void Show();

    /// <summary>Versteckt das Tray‑Icon.</summary>
    void Hide();

    /// <summary>Setzt einen symbolischen Status (z. B. "default", "busy").</summary>
    void SetStatus(string key);

    /// <summary>Zeigt eine Balloon‑Benachrichtigung an.</summary>
    void ShowBalloon(string title, string text, int timeout = 2000);

    /// <summary>Konfiguriert den Service zur Laufzeit (optional).</summary>
    void Configure(Action<TrayIcon> configure);

    /// <summary>Wird bei einfachem Klick ausgelöst (links).</summary>
    event EventHandler? Clicked;

    /// <summary>Wird bei Doppelklick ausgelöst (links).</summary>
    event EventHandler? DoubleClicked;
}
