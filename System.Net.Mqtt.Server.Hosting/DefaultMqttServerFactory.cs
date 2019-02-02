﻿using System.Net.Listeners;
using System.Net.Mqtt.Server.Hosting.Configuration;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace System.Net.Mqtt.Server.Hosting
{
    public class DefaultMqttServerFactory : IMqttServerFactory
    {
        public DefaultMqttServerFactory(ILogger<DefaultMqttServerFactory> logger, IOptions<MqttServiceOptions> options) :
            this(logger, options.Value)
        {
        }

        public DefaultMqttServerFactory(ILogger<DefaultMqttServerFactory> logger, MqttServiceOptions options)
        {
            Logger = logger;
            Options = options;
        }

        public ILogger<DefaultMqttServerFactory> Logger { get; }

        public MqttServiceOptions Options { get; }

        public MqttServer Create()
        {
            var server = new MqttServer();

            foreach (var (name, url) in Options.Endpoints)
            {
                var listener = url switch
                {
                    Uri { Scheme: "tcp" } => (AsyncConnectionListener)new TcpSocketListener(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)),
                    Uri { Scheme: "http" } u => new WebSocketsListener(u, "mqtt", "mqttv3.1"),
                    Uri { Scheme: "ws" } u => new WebSocketsListener(new UriBuilder(u) { Scheme = "http" }.Uri, "mqtt", "mqttv3.1"),
                    _ => throw new ArgumentException("Uri schema not supported.")
                };
                server.RegisterListener(name, listener);
                Logger.LogInformation($"Registered new connection listener '{name}' ({listener.GetType().FullName}) for uri {url}.");
            }

            foreach (var (name, listener) in Options.Listeners)
            {
                server.RegisterListener(name, listener);
                Logger.LogInformation($"Registered new connection listener '{name}' ({listener.GetType().FullName}).");
            }

            return server;
        }
    }
}