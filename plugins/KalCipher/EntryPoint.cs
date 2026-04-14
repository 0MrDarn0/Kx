// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using KalCipher.Config;
using KalCipher.Data;

using Kx.Sdk.Cipher;
using Kx.Sdk.Plugin;

namespace KalCipher;

public sealed class EntryPoint : IPlugin {
    public string Name => "KalCipher";
    private KalCipherConfig? _cipherConfig;

    public void Initialize(IPluginContext context) {
        context.Logger.Info($"{Name} initialized");
        context.Logger.Info($"ApiVersion: {context.ApiVersion}");

        try {
            var cfgPath = Path.Combine(AppContext.BaseDirectory, "Plugins", "KalCipher", "kalcipher.yaml");
            _cipherConfig = new Kx.Sdk.Config.ConfigLoaderAdapter().Load<KalCipherConfig>(cfgPath);
            context.Logger.Info($"Loaded plugin config from {cfgPath}");
        }
        catch (Exception ex) {
            context.Logger.Info($"Failed to load plugin config: {ex.Message}");
            _cipherConfig = new KalCipherConfig();
        }

        // Ensure code pages provider is available (for CP949 / EUC-KR)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // Build encryptor from config and register it
        var pkCfg = _cipherConfig?.Pk ?? new PkConfig();
        var config_encryptor = new Encryptor(System.Text.Encoding.GetEncoding(pkCfg.Codepage)) { Key = pkCfg.CKey };
        context.Services.Register(config_encryptor);
        context.Logger.Info($"Registered Config.pk Encryptor with Key={config_encryptor.Key}");

        // Register the PK cipher service which depends on Encryptor (DI-only)
        context.Services.Register<KalPKCipherService>(new KalPKCipherService(config_encryptor));
        context.Services.Register<IKalPKCipherService>(context.Services.Create<KalPKCipherService>(config_encryptor));
        context.Logger.Info($"Registered {nameof(KalPKCipherService)} and {nameof(IKalPKCipherService)}");

        // Additional logging for service registration
        context.Logger.Info($"Registered {nameof(IKalPKCipherService)} successfully.");
    }


    public void Dispose() { }
}
