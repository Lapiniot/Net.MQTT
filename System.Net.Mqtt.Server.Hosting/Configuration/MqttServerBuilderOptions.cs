using System.Collections.Generic;
using System.Net.Connections;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public class MqttServerBuilderOptions
    {
        public MqttServerBuilderOptions()
        {
            ListenerFactories = new Dictionary<string, Func<IServiceProvider, IAsyncEnumerable<INetworkConnection>>>();
        }

        public IDictionary<string, Func<IServiceProvider, IAsyncEnumerable<INetworkConnection>>> ListenerFactories { get; }
        public int ConnectTimeout { get; set; } = 1000;
    }
}