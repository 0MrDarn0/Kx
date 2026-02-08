// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Net.Http.Headers;

namespace KUpdater.Core;

public class HttpUpdateSource : IUpdateSource {
    private static readonly HttpClient SharedHttp;

    static HttpUpdateSource() {
        var handler = new SocketsHttpHandler {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = int.MaxValue
        };

        SharedHttp = new HttpClient(handler) {
            Timeout = TimeSpan.FromSeconds(60)
        };

        SharedHttp.DefaultRequestHeaders.UserAgent.Clear();
        SharedHttp.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("kUpdater", "1.0"));
    }

    private readonly HttpClient _http;

    public HttpUpdateSource(HttpClient? httpClient = null) => _http = httpClient ?? SharedHttp;

    // Retry configuration
    private const int MaxRetries = 3;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);
    private static readonly Random Jitter = new();

    private static TimeSpan GetDelay(int attempt) {
        var exponential = Math.Pow(2, attempt - 1);
        var jitter = 0.75 + Jitter.NextDouble() * 0.5; // 0.75..1.25
        return TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * exponential * jitter);
    }

    private async Task<T> ExecuteWithRetriesAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct) {
        int attempt = 0;
        while (true) {
            ct.ThrowIfCancellationRequested();
            attempt++;
            try {
                return await operation(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex) when (attempt <= MaxRetries) {
                Console.Error.WriteLine($"Attempt {attempt} failed: {ex.Message}. Retrying...");
                var delay = GetDelay(attempt);
                try {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                continue;
            }
            catch {
                throw;
            }
        }
    }

    public Task<string> GetMetadataJsonAsync(string metadataUrl, CancellationToken ct = default) {
        return ExecuteWithRetriesAsync(async token => {
            using var resp = await _http.GetAsync(metadataUrl, token).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }, ct);
    }

    public Task<Stream> GetPackageStreamAsync(string packageUrl, CancellationToken ct = default) {
        return ExecuteWithRetriesAsync(async token => {
            var response = await _http.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }, ct);
    }

    public Task<long?> GetPackageSizeAsync(string packageUrl, CancellationToken ct = default) {
        // Explicitly specify T = long? so the lambda returns Task<long?> and null is valid
        return ExecuteWithRetriesAsync<long?>(async token => {
            using var req = new HttpRequestMessage(HttpMethod.Head, packageUrl);
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (resp.Content.Headers.ContentLength.HasValue)
                return resp.Content.Headers.ContentLength.Value;
            return (long?)null;
        }, ct);
    }

    public Task<string> GetChangelogAsync(string changelogUrl, CancellationToken ct = default) {
        return ExecuteWithRetriesAsync(async token => {
            using var resp = await _http.GetAsync(changelogUrl, token).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }, ct);
    }
}
