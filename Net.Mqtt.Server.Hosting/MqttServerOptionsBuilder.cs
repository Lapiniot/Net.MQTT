using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.Mqtt.Server.Hosting.Configuration;
using System.Net;

namespace Net.Mqtt.Server.Hosting;

public class MqttServerOptionsBuilder(OptionsBuilder<MqttServerOptions> builder)
{
    public void Listen(IPEndPoint endpoint, string name = null)
    {
        builder.Configure(options => options.Endpoints.Add(name ?? $"mqtt://{endpoint}",
            new(ListenerFactoryExtensions.CreateTcp(endpoint))));
    }

    public void Listen(IPAddress address, int port, string name = null) => Listen(new IPEndPoint(address, port), name);

    public void ListenAnyIP(int port, string name = null) => Listen(IPAddress.IPv6Any, port, name);

    public void ListenLocalhost(int port, string name = null) => Listen(IPAddress.IPv6Loopback, port, name);

    public void ListenUnixSocket(string path, string name = null)
    {
        builder.Configure(options => options.Endpoints.Add(name ?? $"unix://{path}",
            new(ListenerFactoryExtensions.CreateUnixDomainSocket(path))));
    }

    public OptionsBuilder<MqttServerOptions> Builder => builder;
    public IServiceCollection Services => builder.Services;
}