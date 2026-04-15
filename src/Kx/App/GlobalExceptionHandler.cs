// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

namespace Kx.App;

internal static class GlobalExceptionHandler {
    private static bool _registered;
    private static Func<Task>? _shutdownHandler;

    public static void Register() {
        if (_registered)
            return;

        Application.ThreadException += OnThreadException;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _registered = true;
    }

    public static void Unregister() {
        if (!_registered)
            return;
        Application.ThreadException -= OnThreadException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        _registered = false;
    }

    public static void RegisterShutdownHandler(Func<Task> shutdownHandler) {
        ArgumentNullException.ThrowIfNull(shutdownHandler);
        _shutdownHandler = shutdownHandler;
    }

    private static void OnThreadException(object? sender, ThreadExceptionEventArgs e) {
        HandleExceptionAsync("UI Thread Exception", e.Exception).GetAwaiter().GetResult();
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e) {
        if (e.ExceptionObject is Exception exception)
            HandleExceptionAsync("Unhandled Exception", exception).GetAwaiter().GetResult();
    }

    private static async Task HandleExceptionAsync(string source, Exception exception) {
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

        if (_shutdownHandler is not null) {
            try {
                await _shutdownHandler().ConfigureAwait(false);
            }
            catch {
                // Ensure fallback if shutdown handler fails
                Environment.Exit(1);
            }
        }
        else {
            Environment.Exit(1);
        }
    }
}
