using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

internal sealed class KestrelServerOptionsPostSetup(IConfiguration configuration) :
    IPostConfigureOptions<KestrelServerOptions>, IDisposable
{
    private IDisposable? tokenChangeRegistration;

    public void Dispose() => tokenChangeRegistration?.Dispose();

    public void PostConfigure(string? name, KestrelServerOptions options)
    {
        if (options.ConfigurationLoader is { } loader)
        {
            tokenChangeRegistration?.Dispose();
            tokenChangeRegistration = ChangeToken.OnChange(
                changeTokenProducer: () => loader.Configuration.GetReloadToken(),
                changeTokenConsumer: state => ConfigureEndpoints(state.Loader, state.Config),
                state: (Loader: loader, Config: configuration));

            ConfigureEndpoints(loader, configuration);
        }

        static void ConfigureEndpoints(KestrelConfigurationLoader loader, IConfiguration bindingConfiguration)
        {
            foreach (var ep in loader.Configuration.GetSection("Endpoints").GetChildren())
            {
                loader.Endpoint(ep.Key, bindingConfiguration.GetValue<bool?>(ep.Key) switch
                {
                    true => UseConnectionHandler,
                    false => DoNotUseConnectionHandler,
                    _ => UseConnectionHandlerConditionally,
                });
            }
        }

        static void UseConnectionHandlerConditionally(EndpointConfiguration epc)
        {
            if (epc.ConfigSection.GetValue<bool?>("UseMqtt") is true)
            {
                UseConnectionHandler(epc);
            }
        }

        static void UseConnectionHandler(EndpointConfiguration epc)
        {
            if (epc is { IsHttps: true, HttpsOptions: { } httpsOptions })
            {
                epc.ListenOptions.UseHttps(httpsOptions);
            }

            epc.ListenOptions.UseMqttServer();
        }

        static void DoNotUseConnectionHandler(EndpointConfiguration epc)
        {
        }
    }
}