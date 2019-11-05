using System.Collections.Generic;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public class MqttServerOptions
    {
        public MqttServerOptions()
        {
            Endpoints = new Dictionary<string, Uri>();
            Listeners = new Dictionary<string, IConnectionListener>();
        }

        public IDictionary<string, Uri> Endpoints { get; internal set; }
        public IDictionary<string, IConnectionListener> Listeners { get; set; }
    }
}