using System.Collections.Generic;
using System.Net.Connections;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public class MqttServerBuilderOptions
    {
        public MqttServerBuilderOptions()
        {
            Endpoints = new Dictionary<string, Uri>();
            Listeners = new Dictionary<string, IAsyncEnumerable<INetworkConnection>>();
        }

        public IDictionary<string, Uri> Endpoints { get; }
        public IDictionary<string, IAsyncEnumerable<INetworkConnection>> Listeners { get; }
        public int ConnectTimeout { get; set; } = 1000;
    }
}