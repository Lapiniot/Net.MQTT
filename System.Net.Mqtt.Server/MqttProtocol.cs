using System.Net.Pipes;

namespace System.Net.Mqtt.Server
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