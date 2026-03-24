// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Sdk.Events;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(50)]
public class SelfUpdateStep(string rootDirectory) : IUpdateStep {
    private readonly string _rootDir = rootDirectory;
    public string Name => "SelfUpdate";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        string newExe = Path.Combine(_rootDir, "KUpdater_new.exe");
        string bootstrapper = Path.Combine(_rootDir, "Bootstrapper.exe");

        // Prüfen ob neue Version und Bootstrapper vorhanden sind
        if (File.Exists(newExe) && File.Exists(bootstrapper)) {
            eventManager.NotifyAll(new StatusEvent(
                LanguageService.Translate("status.selfupdate_started")));

            try {
                // Pfad der aktuell laufenden EXE holen
                string currentExe = Environment.ProcessPath!;

                // Bootstrapper starten mit Pfaden
                Process.Start(new ProcessStartInfo {
                    FileName = bootstrapper,
                    Arguments = $"\"{currentExe}\" \"{newExe}\"",
                    UseShellExecute = false
                });

                // Alten Updater beenden
                Application.Exit();
            }
            catch (Exception ex) {
                eventManager.NotifyAll(new StatusEvent(
                    LanguageService.Translate("status.selfupdate_failed", ex.Message)));
            }
        }

        await Task.CompletedTask;
    }
}
