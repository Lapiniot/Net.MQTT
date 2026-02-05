using Microsoft.Extensions.Configuration;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

internal sealed class WebSocketConnectionOptionsSetup(IConfiguration configuration) :
    IConfigureNamedOptions<WebSocketConnectionOptions>
{
    public void Configure(string? name, WebSocketConnectionOptions options)
    {
        if (options.SubProtocols is [])
        {
            options.SubProtocols.Add("mqtt");
        }

        BindConfiguration(options, configuration);

        if (!string.IsNullOrEmpty(name))
        {
            BindConfiguration(options, configuration.GetSection(name));
        }

        static void BindConfiguration(WebSocketConnectionOptions options, IConfiguration config)
        {
            if (config.GetSection(nameof(WebSocketConnectionOptions.AllowedOrigins)).Exists())
            {
                options.AllowedOrigins.Clear();
            }

            if (config.GetSection(nameof(WebSocketConnectionOptions.SubProtocols)).Exists())
            {
                options.SubProtocols.Clear();
            }

            config.Bind(options);
        }
    }

    public void Configure(WebSocketConnectionOptions options) => Configure(Options.DefaultName, options);
}