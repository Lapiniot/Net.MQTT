using System.Net.Pipes;

namespace System.Net.Mqtt.Broker
{
    public abstract class MqttProtocol
    {
        protected NetworkPipeReader Reader;

        protected MqttProtocol(NetworkPipeReader reader)
        {
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }
    }
}