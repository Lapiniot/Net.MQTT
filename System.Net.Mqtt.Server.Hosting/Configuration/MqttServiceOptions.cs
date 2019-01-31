using System.Collections.Generic;
using System.Net.Listeners;

namespace System.Net.Mqtt.Server.Hosting.Configuration
{
    public class MqttServiceOptions
    {
        public MqttServiceOptions()
        {
            Endpoints = new Dictionary<string, Uri>();
            Listeners = new Dictionary<string, AsyncConnectionListener>();
        }

        public IDictionary<string, Uri> Endpoints { get; internal set; }
        public IDictionary<string, AsyncConnectionListener> Listeners { get; set; }
    }
}