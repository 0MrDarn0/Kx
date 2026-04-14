// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.ComponentModel;
using System.Diagnostics;

using Kx.Core.Localization;
using Kx.Sdk.Logging;

using KxUpdater.Configuration;

namespace KxUpdater;

internal sealed class UpdaterLauncher {
    private static readonly LanguageKey _invalidUrlErrorKey = UpdaterLanguageKeys.Error.InvalidUrl;
    private static readonly LanguageKey _missingProcessHandleErrorKey = UpdaterLanguageKeys.Error.ProcessNoHandle;

    private readonly ILoggingService _logger;
    private readonly Action<string> _setStatusText;
    private readonly Action _closeWindow;

    public UpdaterLauncher(ILoggingService logger, Action<string> setStatusText, Action closeWindow) {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(setStatusText);
        ArgumentNullException.ThrowIfNull(closeWindow);

        _logger = logger;
        _setStatusText = setStatusText;
        _closeWindow = closeWindow;
    }

    public void ExecuteProcessCommand(ProcessLaunchConfig config, LanguageKey notConfiguredStatusKey, LanguageKey fileMissingStatusKey, LanguageKey startedStatusKey, LanguageKey failedStatusKey) {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.FileName)) {
            _setStatusText(LanguageService.Translate(notConfiguredStatusKey));
            return;
        }

        try {
            var startInfo = CreateProcessStartInfo(config);
            if (RequiresExistingFile(config, startInfo.FileName) && !File.Exists(startInfo.FileName)) {
                _setStatusText(LanguageService.Translate(fileMissingStatusKey, Path.GetFileName(startInfo.FileName)));
                return;
            }

            if (Process.Start(startInfo) is null) {
                _setStatusText(LanguageService.Translate(failedStatusKey, LanguageService.Translate(_missingProcessHandleErrorKey)));
                return;
            }

            _setStatusText(LanguageService.Translate(startedStatusKey));

            if (config.CloseUpdaterOnSuccess)
                _closeWindow();
        }
        catch (InvalidOperationException ex) {
            _logger.Error($"Failed to execute updater command '{config.FileName}'.", ex);
            _setStatusText(LanguageService.Translate(failedStatusKey, ex.Message));
        }
        catch (Win32Exception ex) {
            _logger.Error($"Failed to execute updater command '{config.FileName}'.", ex);
            _setStatusText(LanguageService.Translate(failedStatusKey, ex.Message));
        }
    }

    public void ExecuteWebsiteCommand(string? url) {
        string? trimmedUrl = url?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUrl)) {
            _setStatusText(LanguageService.Translate(UpdaterLanguageKeys.Status.WebsiteNotConfigured));
            return;
        }

        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var targetUri)) {
            _setStatusText(LanguageService.Translate(UpdaterLanguageKeys.Status.WebsiteOpenFailed, LanguageService.Translate(_invalidUrlErrorKey)));
            return;
        }

        try {
            if (Process.Start(new ProcessStartInfo(targetUri.AbsoluteUri) { UseShellExecute = true }) is null) {
                _setStatusText(LanguageService.Translate(UpdaterLanguageKeys.Status.WebsiteOpenFailed, LanguageService.Translate(_missingProcessHandleErrorKey)));
                return;
            }

            _setStatusText(LanguageService.Translate(UpdaterLanguageKeys.Status.WebsiteOpening));
        }
        catch (InvalidOperationException ex) {
            _logger.Error($"Failed to open website '{targetUri.AbsoluteUri}'.", ex);
            _setStatusText(LanguageService.Translate(UpdaterLanguageKeys.Status.WebsiteOpenFailed, ex.Message));
        }
        catch (Win32Exception ex) {
            _logger.Error($"Failed to open website '{targetUri.AbsoluteUri}'.", ex);
            _setStatusText(LanguageService.Translate(UpdaterLanguageKeys.Status.WebsiteOpenFailed, ex.Message));
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(ProcessLaunchConfig config) {
        string fileName = ResolveConfiguredPath(config.FileName, config.ResolveFromAppDirectory);
        string workingDirectory = string.IsNullOrWhiteSpace(config.WorkingDirectory)
            ? ResolveDefaultWorkingDirectory(fileName)
            : ResolveConfiguredPath(config.WorkingDirectory, resolveFromAppDirectory: true);

        return new ProcessStartInfo {
            FileName = fileName,
            Arguments = config.Arguments ?? string.Empty,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };
    }

    private static bool RequiresExistingFile(ProcessLaunchConfig config, string fileName) {
        return config.ResolveFromAppDirectory || Path.IsPathRooted(fileName);
    }

    private static string ResolveConfiguredPath(string path, bool resolveFromAppDirectory) {
        string expandedPath = Environment.ExpandEnvironmentVariables(path.Trim());
        if (Path.IsPathRooted(expandedPath))
            return expandedPath;

        if (!resolveFromAppDirectory)
            return expandedPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedPath));
    }

    private static string ResolveDefaultWorkingDirectory(string fileName) {
        if (!Path.IsPathRooted(fileName))
            return AppContext.BaseDirectory;

        return Path.GetDirectoryName(fileName) ?? AppContext.BaseDirectory;
    }
}
