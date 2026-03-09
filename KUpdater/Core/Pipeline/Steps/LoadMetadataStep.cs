// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Text.Json;
using KUpdater.Abstractions.Events;
using KUpdater.Core.Attributes;
using KUpdater.Core.Event;
using KUpdater.Core.Localization;
using KUpdater.Core.Update;

namespace KUpdater.Core.Pipeline.Steps;

[PipelineStep(10)]
public class LoadMetadataStep(IUpdateSource source, string baseUrl) : IUpdateStep {
    private readonly IUpdateSource _source = source;
    private readonly string _metadataUrl = baseUrl.EndsWith('/') ? baseUrl + "update.json" : baseUrl + "/update.json";
    private readonly string _changelogUrl = baseUrl.EndsWith('/') ? baseUrl + "changelog.txt" : baseUrl + "/changelog.txt";

    public string Name => "LoadMetadata";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        // Status-Event
        eventManager.NotifyAll(new StatusEvent(LanguageService.Translate("status.waiting")));

        // Metadaten laden
        var json = await _source.GetMetadataJsonAsync(_metadataUrl);
        ctx.Metadata = JsonSerializer.Deserialize<UpdateMetadata>(json)!;

        // Changelog laden
        var changelog = await _source.GetChangelogAsync(_changelogUrl);
        eventManager.NotifyAll(new ChangelogEvent(changelog));
    }
}
