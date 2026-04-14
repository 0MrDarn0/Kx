// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Threading.Channels;
using KUpdater.Abstractions.Lifecycle;
using KUpdater.Abstractions.Logging;

namespace KUpdater.Core.Logging;

public sealed class AsyncLogSink : ILogSink, IAsyncDisposable, IShutdownAware {

    private readonly ILogSink _inner;
    private readonly Channel<LogEntry> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    public AsyncLogSink(ILogSink inner, int capacity = 1024) {
        _inner = inner;

        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(capacity) {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _worker = Task.Run(ProcessQueueAsync);
    }

    public void Write(string category, LogLevel level, string message, Exception? ex) {
        _channel.Writer.TryWrite(new LogEntry(category, level, message, ex));
    }

    private async Task ProcessQueueAsync() {
        try {
            while (await _channel.Reader.WaitToReadAsync(_cts.Token)) {
                while (_channel.Reader.TryRead(out var entry)) {
                    _inner.Write(entry.Category, entry.Level, entry.Message, entry.Exception);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask ShutdownAsync() {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync() {
        _cts.Cancel();
        _channel.Writer.Complete();
        await _worker;
        _cts.Dispose();
    }
}
