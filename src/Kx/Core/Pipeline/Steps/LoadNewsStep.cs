// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Core.Update;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(15)]
public class LoadNewsStep(IUpdateSource source, string baseUrl) : IUpdateStep {
    private const string NewsFileName = "news.yaml";

    private readonly IUpdateSource _source = source;
    private readonly string _baseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

    public string Name => "LoadNews";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        try {
            string news = await LoadNewsContentAsync(ct);
            eventManager.NotifyAll(new ChangelogEvent(news));
        }
        catch (Exception ex) {
            eventManager.NotifyAll(new StatusEvent(
                LanguageService.Translate(KxLanguageKeys.Status.ChangelogFailed, ex.Message)
            ));
        }
    }

    private Task<string> LoadNewsContentAsync(CancellationToken ct) {
        string newsUrl = _baseUrl + NewsFileName;
        return _source.GetChangelogAsync(newsUrl, ct);
    }
}
