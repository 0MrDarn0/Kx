// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Reflection;
using Kx.Sdk.Events;
using Kx.Core.Attributes;
using Kx.Core.Event;
using Kx.Core.Localization;
using Kx.Core.Update;

namespace Kx.Core.Pipeline;

public class UpdaterPipelineRunner {
    private readonly IEventManager _eventManager;
    private readonly List<IUpdateStep> _steps = [];

    public UpdaterPipelineRunner(IEventManager eventManager, IUpdateSource source, string baseUrl, string rootDir) {
        _eventManager = eventManager;

        var stepTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(IUpdateStep).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => new
            {
                Type = t,
                Attr = t.GetCustomAttribute<PipelineStepAttribute>()
            })
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Order);

        foreach (var stepInfo in stepTypes) {
            var ctor = stepInfo.Type.GetConstructors().First();
            var args = ctor.GetParameters().Select(p =>
            {
                if (p.ParameterType == typeof(IUpdateSource))
                    return (object)source;
                if (p.ParameterType == typeof(string) && p.Name!.Contains("base", StringComparison.OrdinalIgnoreCase))
                    return (object)baseUrl;
                if (p.ParameterType == typeof(string) && p.Name!.Contains("root", StringComparison.OrdinalIgnoreCase))
                    return (object)rootDir;
                throw new InvalidOperationException($"Unknown ctor argument {p.Name} in {stepInfo.Type.Name}");
            }).ToArray();

            var step = (IUpdateStep)Activator.CreateInstance(stepInfo.Type, args)!;
            _steps.Add(step);
        }
    }

    public async Task<bool> CheckForUpdatesAsync(string rootDir, CancellationToken ct = default) {
        var context = new UpdateContext(rootDir);
        bool updateRequired = false;

        void OnUpdateRequired(UpdateRequired _) => updateRequired = true;

        _eventManager.Register<UpdateRequired>(OnUpdateRequired);

        try {
            await RunStepsAsync(context, _steps.TakeWhile(step => step.Name is not "DownloadAndExtract" and not "SaveVersion" and not "SelfUpdate"), ct);
        }
        catch (OperationCanceledException) {
        }
        catch (Exception ex) {
            _eventManager.NotifyAll(new StatusEvent(
                LanguageService.Translate(KxLanguageKeys.Status.UpdateFailed, ex.Message)
            ));
        }
        finally {
            _eventManager.Unregister<UpdateRequired>(OnUpdateRequired);
        }

        return updateRequired;
    }

    public async Task RunAsync(string rootDir, CancellationToken ct = default) {
        var context = new UpdateContext(rootDir);

        try {
            await RunStepsAsync(context, _steps, ct);
        }
        catch (OperationCanceledException) {
            // Cancelled: treat as no update / user cancelled
        }
        catch (Exception ex) {
            _eventManager.NotifyAll(new StatusEvent(
                LanguageService.Translate(KxLanguageKeys.Status.UpdateFailed, ex.Message)
            ));
        }
    }

    private async Task RunStepsAsync(UpdateContext context, IEnumerable<IUpdateStep> steps, CancellationToken ct) {
        _eventManager.NotifyAll(new UpdatePipelineStarted());

        foreach (var step in steps) {
            ct.ThrowIfCancellationRequested();
            _eventManager.NotifyAll(new UpdateStepStarted(step.Name));
            await step.ExecuteAsync(context, _eventManager, ct);
            _eventManager.NotifyAll(new UpdateStepCompleted(step.Name));
        }

        _eventManager.NotifyAll(new UpdatePipelineCompleted());
    }
}
