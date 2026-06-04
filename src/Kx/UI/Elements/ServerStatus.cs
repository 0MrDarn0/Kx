// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Sockets;

using Kx.App;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;

using SkiaSharp;

namespace Kx.UI.Elements;

public sealed class ServerStatus : UIElement {
    private const float DefaultFontSize = 11f;
    private const float DefaultIconFontSize = 12f;
    private const float DefaultIndicatorSpacing = 6f;
    private const int MinimumTimeoutMilliseconds = 250;
    private static readonly ConcurrentDictionary<MonitorCacheKey, CachedMonitorState> _monitorStateCache = [];

    private readonly SKPaint _textPaint = new() { IsAntialias = true, Color = SKColors.White };
    private string _fontFamily = "Segoe UI";
    private float _fontSize = DefaultFontSize;
    private bool _bold;
    private bool _italic;
    private SKTypeface? _customTypeface;
    private string _iconFontFamily = "Segoe UI Symbol";
    private float _iconFontSize = DefaultIconFontSize;
    private SKTypeface? _typeface;
    private SKFont? _font;
    private SKTypeface? _iconTypeface;
    private SKFont? _iconFont;
    private string _displayName = string.Empty;
    private bool _monitoringEnabled = true;
    private string _host = string.Empty;
    private int _port;
    private int _checkIntervalSeconds = 15;
    private int _connectTimeoutMilliseconds = 1500;
    private bool _showIndicator = true;
    private bool _showText = true;
    private float _indicatorSpacing = DefaultIndicatorSpacing;
    private string _checkingText = "{0}: checking...";
    private string _onlineText = "{0}: online";
    private string _offlineText = "{0}: offline";
    private string _timeoutText = "{0}: timeout";
    private string _checkingIndicator = "◌";
    private string _onlineIndicator = "●";
    private string _offlineIndicator = "●";
    private string _timeoutIndicator = "●";
    private string? _checkingImage;
    private string? _onlineImage;
    private string? _offlineImage;
    private string? _timeoutImage;
    private SKBitmap? _checkingBitmap;
    private SKBitmap? _onlineBitmap;
    private SKBitmap? _offlineBitmap;
    private SKBitmap? _timeoutBitmap;
    private KxColor _checkingColor = KxColor.Parse("#DAA520");
    private KxColor _onlineColor = KxColor.Parse("#4CAF50");
    private KxColor _offlineColor = KxColor.Parse("#E57373");
    private KxColor _timeoutColor = KxColor.Parse("#FFB74D");
    private string _currentText = string.Empty;
    private string _currentIndicator = string.Empty;
    private SKColor _currentColor = SKColors.White;
    private SKBitmap? _currentBitmap;
    private ServerStatusState _currentState = ServerStatusState.Disabled;
    private CancellationTokenSource? _monitorCts;
    private Task? _monitorTask;

    public ServerStatus(IVisualContext context, string id) : base(context, id) {
        Padding = new Kx.Sdk.UI.Layout.Thickness(2, 0, 2, 0);
        UpdateFonts();
        UpdatePresentation();
    }

    public string DisplayName {
        get => _displayName;
        set {
            _displayName = value?.Trim() ?? string.Empty;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string Host {
        get => _host;
        set {
            _host = value?.Trim() ?? string.Empty;
            RestartMonitoring();
        }
    }

    public bool MonitoringEnabled {
        get => _monitoringEnabled;
        set {
            if (_monitoringEnabled == value)
                return;

            _monitoringEnabled = value;
            RestartMonitoring();
        }
    }

    public int Port {
        get => _port;
        set {
            _port = value;
            RestartMonitoring();
        }
    }

    public int CheckIntervalSeconds {
        get => _checkIntervalSeconds;
        set {
            _checkIntervalSeconds = Math.Max(1, value);
            RestartMonitoring();
        }
    }

    public int ConnectTimeoutMilliseconds {
        get => _connectTimeoutMilliseconds;
        set {
            _connectTimeoutMilliseconds = Math.Max(MinimumTimeoutMilliseconds, value);
            RestartMonitoring();
        }
    }

    public string FontFamily {
        get => _fontFamily;
        set {
            _fontFamily = string.IsNullOrWhiteSpace(value) ? "Segoe UI" : value;
            UpdateFonts();
            Invalidate();
        }
    }

    public float FontSize {
        get => _fontSize;
        set {
            _fontSize = value > 0 ? value : DefaultFontSize;
            UpdateFonts();
            Invalidate();
        }
    }

    public bool Bold {
        get => _bold;
        set {
            _bold = value;
            UpdateFonts();
            Invalidate();
        }
    }

    public bool Italic {
        get => _italic;
        set {
            _italic = value;
            UpdateFonts();
            Invalidate();
        }
    }

    /// <summary>
    /// Sets an explicit typeface that overrides family-name lookup for the server status text.
    /// </summary>
    public void SetFontTypeface(SKTypeface? typeface) {
        _customTypeface?.Dispose();
        _customTypeface = typeface;
        UpdateFonts();
        Invalidate();
    }

    public string IconFontFamily {
        get => _iconFontFamily;
        set {
            _iconFontFamily = string.IsNullOrWhiteSpace(value) ? "Segoe UI Symbol" : value;
            UpdateFonts();
            Invalidate();
        }
    }

    public float IconFontSize {
        get => _iconFontSize;
        set {
            _iconFontSize = value > 0 ? value : DefaultIconFontSize;
            UpdateFonts();
            Invalidate();
        }
    }

    public bool ShowIndicator {
        get => _showIndicator;
        set {
            _showIndicator = value;
            Invalidate();
        }
    }

    public bool ShowText {
        get => _showText;
        set {
            _showText = value;
            Invalidate();
        }
    }

    public float IndicatorSpacing {
        get => _indicatorSpacing;
        set {
            _indicatorSpacing = Math.Max(0f, value);
            Invalidate();
        }
    }

    public string CheckingText {
        get => _checkingText;
        set {
            _checkingText = string.IsNullOrWhiteSpace(value) ? "{0}: checking..." : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string OnlineText {
        get => _onlineText;
        set {
            _onlineText = string.IsNullOrWhiteSpace(value) ? "{0}: online" : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string OfflineText {
        get => _offlineText;
        set {
            _offlineText = string.IsNullOrWhiteSpace(value) ? "{0}: offline" : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string TimeoutText {
        get => _timeoutText;
        set {
            _timeoutText = string.IsNullOrWhiteSpace(value) ? "{0}: timeout" : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string CheckingIndicator {
        get => _checkingIndicator;
        set {
            _checkingIndicator = string.IsNullOrWhiteSpace(value) ? "◌" : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string OnlineIndicator {
        get => _onlineIndicator;
        set {
            _onlineIndicator = string.IsNullOrWhiteSpace(value) ? "●" : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string OfflineIndicator {
        get => _offlineIndicator;
        set {
            _offlineIndicator = string.IsNullOrWhiteSpace(value) ? "●" : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string TimeoutIndicator {
        get => _timeoutIndicator;
        set {
            _timeoutIndicator = string.IsNullOrWhiteSpace(value) ? "●" : value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public KxColor CheckingColor {
        get => _checkingColor;
        set {
            _checkingColor = value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public KxColor OnlineColor {
        get => _onlineColor;
        set {
            _onlineColor = value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public KxColor OfflineColor {
        get => _offlineColor;
        set {
            _offlineColor = value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public KxColor TimeoutColor {
        get => _timeoutColor;
        set {
            _timeoutColor = value;
            UpdatePresentation();
            Invalidate();
        }
    }

    public string? CheckingImage {
        get => _checkingImage;
        set => SetImageResource(ref _checkingImage, ref _checkingBitmap, value);
    }

    public string? OnlineImage {
        get => _onlineImage;
        set => SetImageResource(ref _onlineImage, ref _onlineBitmap, value);
    }

    public string? OfflineImage {
        get => _offlineImage;
        set => SetImageResource(ref _offlineImage, ref _offlineBitmap, value);
    }

    public string? TimeoutImage {
        get => _timeoutImage;
        set => SetImageResource(ref _timeoutImage, ref _timeoutBitmap, value);
    }

    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
        UpdateFonts();
    }

    public override void Measure(float dpi) {
        if (FixedBounds is System.Drawing.Rectangle fixedBounds) {
            DesiredSize = new System.Drawing.Size(
                fixedBounds.Width + (int)(Margin.Horizontal * dpi),
                fixedBounds.Height + (int)(Margin.Vertical * dpi));
            return;
        }

        DesiredSize = new System.Drawing.Size((int)(220 * dpi), (int)(24 * dpi));
    }

    protected override void OnDraw(IKxCanvas canvas) {
        var skCanvas = canvas.As<SKCanvas>();
        if (skCanvas is null)
            return;

        if (!Visible || _font is null || _iconFont is null)
            return;

        var contentRect = ContentRect;
        float centerY = contentRect.Top + (contentRect.Height / 2f);
        float x = contentRect.Left;

        if (ShowIndicator) {
            if (_currentBitmap is not null) {
                float imageSize = Math.Min(contentRect.Height, 16f * DpiScale);
                float imageTop = centerY - (imageSize / 2f);
                skCanvas.DrawBitmap(_currentBitmap, new SKRect(x, imageTop, x + imageSize, imageTop + imageSize));
                x += imageSize + IndicatorSpacing;
            } else if (!string.IsNullOrWhiteSpace(_currentIndicator)) {
                using var indicatorPaint = new SKPaint {
                    IsAntialias = true,
                    Color = _currentColor
                };

                float baseline = centerY - ((_iconFont.Metrics.Ascent + _iconFont.Metrics.Descent) / 2f);
                skCanvas.DrawText(_currentIndicator, x, baseline, _iconFont, indicatorPaint);

                _iconFont.MeasureText(_currentIndicator, out SKRect indicatorBounds);
                x += indicatorBounds.Width + IndicatorSpacing;
            }
        }

        if (!ShowText || string.IsNullOrWhiteSpace(_currentText))
            return;

        _textPaint.Color = _currentColor;
        float textBaseline = centerY - ((_font.Metrics.Ascent + _font.Metrics.Descent) / 2f);
        skCanvas.DrawText(_currentText, x, textBaseline, _font, _textPaint);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            StopMonitoring();
            _customTypeface?.Dispose();
            _typeface?.Dispose();
            _font?.Dispose();
            _iconTypeface?.Dispose();
            _iconFont?.Dispose();
            _checkingBitmap?.Dispose();
            _onlineBitmap?.Dispose();
            _offlineBitmap?.Dispose();
            _timeoutBitmap?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RestartMonitoring() {
        StopMonitoring();

        if (!_monitoringEnabled || string.IsNullOrWhiteSpace(_host) || _port <= 0 || _port > 65535) {
            SetState(ServerStatusState.Disabled);
            return;
        }

        bool hasCachedState = TryGetCachedState(out var cachedState);
        if (hasCachedState)
            SetState(cachedState);

        _monitorCts = new CancellationTokenSource();
        _monitorTask = MonitorAsync(_monitorCts.Token, skipImmediateProbe: hasCachedState);
    }

    private void StopMonitoring() {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
        _monitorTask = null;
    }

    private async Task MonitorAsync(CancellationToken cancellationToken, bool skipImmediateProbe) {
        if (!skipImmediateProbe)
            SetState(ServerStatusState.Checking);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _checkIntervalSeconds)));

        try {
            if (!skipImmediateProbe) {
                await ProbeAndApplyStateAsync(cancellationToken).ConfigureAwait(false);
            }

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)) {
                SetState(ServerStatusState.Checking);
                await ProbeAndApplyStateAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        }
    }

    private async Task ProbeAndApplyStateAsync(CancellationToken cancellationToken) {
        ServerStatusState state = await ProbeAsync(cancellationToken).ConfigureAwait(false);
        _monitorStateCache[GetMonitorCacheKey()] = new CachedMonitorState(state, DateTimeOffset.UtcNow);
        SetState(state);
    }

    private async Task<ServerStatusState> ProbeAsync(CancellationToken cancellationToken) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Max(MinimumTimeoutMilliseconds, _connectTimeoutMilliseconds));

        try {
            using TcpClient client = new();
            await client.ConnectAsync(_host, _port, timeoutCts.Token).ConfigureAwait(false);
            return ServerStatusState.Online;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return ServerStatusState.Offline;
        }
        catch (SocketException) {
            return ServerStatusState.Offline;
        }
        catch (InvalidOperationException) {
            return ServerStatusState.Offline;
        }
    }

    private void SetState(ServerStatusState state) {
        void apply() {
            _currentState = state;
            UpdatePresentation();
            Invalidate();
        }

        if (Context.UiThread.InvokeRequired)
            Context.UiThread.BeginInvoke(new Action(apply));
        else
            apply();
    }

    private void UpdateFonts() {
        _font?.Dispose();
        _iconTypeface?.Dispose();
        _iconFont?.Dispose();

        if (_customTypeface is null) {
            _typeface?.Dispose();
            _typeface = CreateTypeface(_fontFamily, _bold, _italic);
        } else {
            _typeface?.Dispose();
            _typeface = null;
        }

        _font = new SKFont(_customTypeface ?? _typeface ?? SKTypeface.Default, _fontSize);
        _iconTypeface = SKTypeface.FromFamilyName(_iconFontFamily);
        _iconFont = new SKFont(_iconTypeface, _iconFontSize);
    }

    private void UpdatePresentation() {
        string resolvedName = ResolveDisplayName();

        (_currentColor, _currentIndicator, _currentBitmap, string template) = _currentState switch {
            ServerStatusState.Checking => (ToSkColor(_checkingColor), _checkingIndicator, _checkingBitmap, _checkingText),
            ServerStatusState.Online => (ToSkColor(_onlineColor), _onlineIndicator, _onlineBitmap, _onlineText),
            ServerStatusState.Timeout => (ToSkColor(_timeoutColor), _timeoutIndicator, _timeoutBitmap, _timeoutText),
            ServerStatusState.Offline => (ToSkColor(_offlineColor), _offlineIndicator, _offlineBitmap, _offlineText),
            _ => (SKColors.Transparent, string.Empty, null, string.Empty)
        };

        _currentText = string.IsNullOrWhiteSpace(template)
            ? string.Empty
            : FormatStatusText(template, resolvedName);
    }

    private string ResolveDisplayName() {
        if (!string.IsNullOrWhiteSpace(_displayName))
            return _displayName;

        if (!string.IsNullOrWhiteSpace(_host) && _port > 0)
            return $"{_host}:{_port}";

        return "Server";
    }

    private static SKColor ToSkColor(KxColor color) => new(color.R, color.G, color.B, color.A);

    private void SetImageResource(ref string? resourceIdField, ref SKBitmap? bitmapField, string? value) {
        string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (string.Equals(resourceIdField, normalizedValue, StringComparison.Ordinal))
            return;

        resourceIdField = normalizedValue;
        bitmapField?.Dispose();
        bitmapField = ResolveBitmap(normalizedValue);
        UpdatePresentation();
        Invalidate();
    }

    private SKBitmap? ResolveBitmap(string? resourceId) {
        if (string.IsNullOrWhiteSpace(resourceId) || Context is not WindowContext windowContext)
            return null;

        return windowContext.Resources.TryGetSkiaBitmap(resourceId);
    }

    private static string FormatStatusText(string template, string displayName) {
        try {
            return string.Format(CultureInfo.InvariantCulture, template, displayName);
        }
        catch (FormatException) {
            return $"{displayName}: {template}";
        }
    }

    private static SKTypeface CreateTypeface(string fontFamily, bool bold, bool italic) {
        SKFontStyleWeight weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        return SKTypeface.FromFamilyName(fontFamily, weight, SKFontStyleWidth.Normal, slant);
    }

    private MonitorCacheKey GetMonitorCacheKey() {
        return new MonitorCacheKey(_host.Trim(), _port);
    }

    private bool TryGetCachedState(out ServerStatusState state) {
        state = default;

        if (!_monitorStateCache.TryGetValue(GetMonitorCacheKey(), out var cachedState))
            return false;

        if (DateTimeOffset.UtcNow - cachedState.Timestamp > TimeSpan.FromSeconds(Math.Max(1, _checkIntervalSeconds)))
            return false;

        state = cachedState.State;
        return true;
    }

    private enum ServerStatusState {
        Disabled,
        Checking,
        Online,
        Offline,
        Timeout
    }

    private readonly record struct CachedMonitorState(ServerStatusState State, DateTimeOffset Timestamp);
    private readonly record struct MonitorCacheKey(string Host, int Port);
}
