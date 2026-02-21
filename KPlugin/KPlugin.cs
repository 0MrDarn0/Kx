using KUpdater.Abstractions.Plugin;

namespace KUpdater.Plugin;

public sealed class KPlugin : IPlugin
{
    public string Name => "KPlugin";

    public void Initialize(IPluginContext context)
    {
        context.Logger.Info("KPlugin initialized");
        context.Logger.Info($"Host ApiVersion: {context.ApiVersion}");
    }

    public void Dispose()
    {
        // optional cleanup
    }
}
