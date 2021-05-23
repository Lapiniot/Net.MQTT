using System.Collections.Generic;
using System.Net.Connections;
using System.Net.Listeners;
using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting
{
    public static class MqttServerHostingExtensions
    {
        private const string RootSectionName = "MQTT";
        private static string[] subProtocols;

        public static IServiceCollection ConfigureMqttService(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .Configure<MqttServerBuilderOptions>(configuration)
                .PostConfigure<MqttServerBuilderOptions>(options =>
                {
                    foreach(var item in configuration.GetSection("Endpoints").GetChildren())
                    {
                        if(item.Value is not null)
                        {
                            options.Listeners.Add(item.Key, CreateListener(new Uri(item.Value)));
                        }
                        else
                        {
                            options.Listeners.Add(item.Key, CreateListener(new Uri(item.GetValue<string>("Url"))));
                        }
                    }
                });
        }

        public static IHostBuilder ConfigureMqttService(this IHostBuilder hostBuilder)
        {
            if(hostBuilder is null) throw new ArgumentNullException(nameof(hostBuilder));

            return hostBuilder.ConfigureServices((context, services) =>
                services.ConfigureMqttService(context.Configuration.GetSection(RootSectionName)));
        }

        public static IHostBuilder UseMqttService(this IHostBuilder hostBuilder)
        {
            if(hostBuilder is null) throw new ArgumentNullException(nameof(hostBuilder));

            return hostBuilder.ConfigureServices((context, services) =>
                services.AddDefaultMqttServerBuilder().AddMqttService());
        }

        public static IServiceCollection AddDefaultMqttServerBuilder(this IServiceCollection services)
        {
            return services.Replace(ServiceDescriptor.Transient<IMqttServerBuilder, MqttServerBuilder>());
        }

        public static IServiceCollection AddMqttService(this IServiceCollection services)
        {
            return services.AddHostedService<GenericMqttHostService>();
        }

        public static IServiceCollection AddMqttAuthentication<T>(this IServiceCollection services)
            where T : class, IMqttAuthenticationHandler
        {
            return services.AddTransient<IMqttAuthenticationHandler, T>();
        }

        public static IServiceCollection AddMqttAuthentication(this IServiceCollection services,
            Func<IServiceProvider, IMqttAuthenticationHandler> implementationFactory)
        {
            return services.AddTransient<IMqttAuthenticationHandler>(implementationFactory);
        }

        private static IAsyncEnumerable<INetworkConnection> CreateListener(Uri url)
        {
            return url switch
            {
                { Scheme: "tcp" } => new TcpSocketListener(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)),
                { Scheme: "http", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"{u.Scheme}://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "http" } u => new WebSocketListener(new[] { $"{u.Scheme}://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "ws", Host: "0.0.0.0" } u => new WebSocketListener(new[] { $"http://+:{u.Port}{u.PathAndQuery}" }, GetSubProtocols()),
                { Scheme: "ws" } u => new WebSocketListener(new[] { $"http://{u.Authority}{u.PathAndQuery}" }, GetSubProtocols()),
                _ => throw new ArgumentException("Uri schema not supported.")
            };
        }

        private static string[] GetSubProtocols()
        {
            return subProtocols ??= new string[] { "mqtt", "mqttv3.1" };
        }
    }
}