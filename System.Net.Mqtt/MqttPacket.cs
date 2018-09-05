using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("System.Net.Mqtt.Tests")]

namespace System.Net.Mqtt
{
    public abstract class MqttPacket
    {
        public QoSLevel QoSLevel { get; set; }

        public bool Duplicate { get; set; }

        public bool Retain { get; set; }

        public abstract Memory<byte> GetBytes();
    }
}