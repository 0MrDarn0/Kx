// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

namespace Kx.Core.Plugin;

/// <summary>
/// Emits formatted diagnostics for plugin loading and runtime orchestration.
/// </summary>
public sealed class PluginDiagnostics {
    private readonly Action<string> _traceWriter;
    private readonly Action<string> _errorWriter;

    /// <summary>
    /// Initializes a new diagnostics sink for plugin-related trace and error output.
    /// </summary>
    /// <param name="traceWriter">The writer used for trace diagnostics.</param>
    /// <param name="errorWriter">The writer used for error diagnostics.</param>
    public PluginDiagnostics(Action<string>? traceWriter = null, Action<string>? errorWriter = null) {
        _traceWriter = traceWriter ?? (message => Debug.WriteLine(message));
        _errorWriter = errorWriter ?? (message => Console.Error.WriteLine(message));
    }

    /// <summary>
    /// Writes a trace diagnostic for the specified plugin subsystem.
    /// </summary>
    /// <param name="source">The subsystem that emits the diagnostic.</param>
    /// <param name="message">The diagnostic message.</param>
    public void Trace(string source, string message) {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _traceWriter($"[{source}] {message}");
    }

    /// <summary>
    /// Writes an error diagnostic for the specified plugin subsystem.
    /// </summary>
    /// <param name="source">The subsystem that emits the diagnostic.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="exception">The optional exception to append.</param>
    public void Error(string source, string message, Exception? exception = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _errorWriter(exception is null
            ? $"[{source}] {message}"
            : $"[{source}] {message}: {exception}");
    }
}
