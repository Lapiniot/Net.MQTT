using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OOs.Net.Connections;

#nullable enable

namespace Net.Mqtt.Server.Hosting;

public class MqttServerOptionsBuilder(OptionsBuilder<MqttServerOptions> builder)
{
    public void Listen(IPEndPoint endpoint, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        builder.Configure(options => options.Endpoints.Add(name ?? $"mqtt://{endpoint}", new(endpoint)));
    }

    public void Listen(IPEndPoint endPoint, Action<MqttEndpointBuilder> configure, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Configure(options =>
        {
            var epBuilder = new MqttEndpointBuilder(new MqttEndpoint(endPoint), options);
            configure(epBuilder);
            var schema = epBuilder.EndPoint.Certificate is { } ? "mqtts" : "mqtt";
            options.Endpoints.Add(name ?? $"{schema}://{epBuilder.EndPoint.EndPoint}", epBuilder.EndPoint);
        });
    }

    public void Listen(IPAddress address, int port, string? name = null) =>
        Listen(new IPEndPoint(address, port), name);

    public void Listen(IPAddress address, int port, Action<MqttEndpointBuilder> configure, string? name = null) =>
        Listen(new IPEndPoint(address, port), configure, name);

    public void ListenAnyIP(int port, string? name = null) =>
        Listen(IPAddress.IPv6Any, port, name);

    public void ListenAnyIP(int port, Action<MqttEndpointBuilder> configure, string? name = null) =>
        Listen(IPAddress.IPv6Any, port, configure, name);

    public void ListenLocalhost(int port, string? name = null) =>
        Listen(IPAddress.IPv6Loopback, port, name);

    public void ListenLocalhost(int port, Action<MqttEndpointBuilder> configure, string? name = null) =>
        Listen(IPAddress.IPv6Loopback, port, configure, name);

    public void ListenUnixSocket(string path, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        builder.Configure(options => options.Endpoints.Add(name ?? $"unix://{path}",
            new(new UnixDomainSocketEndPoint(path))));
    }

    public void UseListenerFactory(Func<IAsyncEnumerable<TransportConnection>> factory, string name)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrEmpty(name);
        builder.Configure(options => options.Endpoints.Add(name, new(factory)));
    }

    public void UseListenerFactory(Func<IServiceProvider, IAsyncEnumerable<TransportConnection>> factory, string name)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrEmpty(name);
        builder.Configure<IServiceProvider>((options, sp) => options.Endpoints.Add(name, new(() => factory(sp))));
    }

    public OptionsBuilder<MqttServerOptions> Builder => builder;
    public IServiceCollection Services => builder.Services;
}