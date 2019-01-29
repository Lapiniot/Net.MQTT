using System.Collections.Generic;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public class MqttServiceOptions
    {
        public MqttServiceOptions()
        {
            Endpoints = new Dictionary<string, Uri>();
            Listeners = new Dictionary<string, ConnectionListener>();
        }

        public IDictionary<string, Uri> Endpoints { get; internal set; }
        public IDictionary<string, ConnectionListener> Listeners { get; set; }
    }
}