using System.Collections.Generic;
using System.Net.Connections;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public class MqttServerBuilderOptions
    {
        public MqttServerBuilderOptions()
        {
            Listeners = new Dictionary<string, IAsyncEnumerable<INetworkConnection>>();
        }

        public IDictionary<string, IAsyncEnumerable<INetworkConnection>> Listeners { get; }
        public int ConnectTimeout { get; set; } = 1000;
    }
}