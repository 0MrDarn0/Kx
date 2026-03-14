// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Core.Update;

namespace Kx.Core.Pipeline.Steps;

[PipelineStep(15)]
public class LoadChangelogStep(IUpdateSource source, string baseUrl) : IUpdateStep {
    private readonly IUpdateSource _source = source;
    private readonly string _baseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

    public string Name => "LoadChangelog";

    public async Task ExecuteAsync(UpdateContext ctx, IEventManager eventManager, CancellationToken ct = default) {
        try {
            string changelogUrl = _baseUrl + "changelog.txt";
            string changelog = await _source.GetChangelogAsync(changelogUrl, ct);

            // Event feuern → landet in UIState
            eventManager.NotifyAll(new ChangelogEvent(changelog));
        }
        catch (Exception ex) {
            eventManager.NotifyAll(new StatusEvent(
                LanguageService.Translate("status.changelog_failed", ex.Message)
            ));
        }
    }
}
