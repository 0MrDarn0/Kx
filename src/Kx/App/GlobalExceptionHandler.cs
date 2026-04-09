// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.App;

internal static class GlobalExceptionHandler {
    private static bool _registered;

    public static void Register() {
        if (_registered)
            return;

        Application.ThreadException += OnThreadException;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _registered = true;
    }

    private static void OnThreadException(object? sender, ThreadExceptionEventArgs e) {
        HandleException("UI Thread Exception", e.Exception);
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e) {
        if (e.ExceptionObject is Exception exception)
            HandleException("Unhandled Exception", exception);
    }

    private static void HandleException(string source, Exception exception) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(exception);

        string message = $"{source}:\n\n{exception.GetType().Name}: {exception.Message}\n\nStackTrace:\n{exception.StackTrace}";

        try {
            MessageBox.Show(
                message,
                "Critical Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (InvalidOperationException) {
            Console.Error.WriteLine(message);
        }

        Environment.Exit(1);
    }
}
