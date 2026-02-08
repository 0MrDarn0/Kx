// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Net.Http.Headers;

namespace KUpdater.Core;

public class HttpUpdateSource : IUpdateSource {
    // Shared HttpClient with a configured SocketsHttpHandler to control pooling and DNS refresh
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

        // Optional: set a default UserAgent for server logs and diagnostics
        SharedHttp.DefaultRequestHeaders.UserAgent.Clear();
        SharedHttp.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("kUpdater", "1.0"));
    }

    private readonly HttpClient _http;

    // Allow injection for tests or advanced configuration; fallback to shared client
    public HttpUpdateSource(HttpClient? httpClient = null) => _http = httpClient ?? SharedHttp;

    public async Task<string> GetMetadataJsonAsync(string metadataUrl) {
        return await _http.GetStringAsync(metadataUrl).ConfigureAwait(false);
    }

    public async Task<Stream> GetPackageStreamAsync(string packageUrl) {
        var response = await _http.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    public async Task<long?> GetPackageSizeAsync(string packageUrl) {
        using var resp = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, packageUrl)).ConfigureAwait(false);
        if (resp.Content.Headers.ContentLength.HasValue)
            return resp.Content.Headers.ContentLength.Value;
        return null;
    }

    public async Task<string> GetChangelogAsync(string changelogUrl) {
        return await _http.GetStringAsync(changelogUrl).ConfigureAwait(false);
    }
}
