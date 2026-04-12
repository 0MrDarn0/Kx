// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Core.Update;
using Kx.Sdk.Events;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(50)]
public class SelfUpdateStep(string rootDirectory) : IUpdateStep {
    private readonly string _rootDir = rootDirectory;
    public string Name => "SelfUpdate";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        string? currentExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExe)) {
            await Task.CompletedTask;
            return;
        }

        string newExe = Path.Combine(
            _rootDir,
            Path.GetFileNameWithoutExtension(currentExe) + UpdaterConstants.PendingSelfUpdateSuffix + Path.GetExtension(currentExe));
        string bootstrapper = Path.Combine(_rootDir, "Bootstrapper.exe");

        if (File.Exists(newExe) && File.Exists(bootstrapper)) {
            eventManager.NotifyAll(new StatusEvent(
                LanguageService.Translate(KxLanguageKeys.Status.SelfUpdateStarted)));

            try {
                Process.Start(new ProcessStartInfo {
                    FileName = bootstrapper,
                    Arguments = $"\"{currentExe}\" \"{newExe}\"",
                    UseShellExecute = false
                });

                Application.Exit();
            }
            catch (Exception ex) {
                eventManager.NotifyAll(new StatusEvent(
                    LanguageService.Translate(KxLanguageKeys.Status.SelfUpdateFailed, ex.Message)));
            }
        }

        await Task.CompletedTask;
    }
}
