using System.IO.Pipelines;
using System.Net.Connections;

namespace System.Net.Mqtt.Server
{
    /// <summary>
    /// Represents base abstract MQTT version specific protocol hub implementation which is in charge:
    /// 1. Acts as a client session factory for specific version
    /// 2. Distributes incoming messages from server across internally maintained list of sessions, according to the specific
    /// protocol version rules
    /// </summary>
    public abstract class MqttProtocolHub
    {
        public abstract int ProtocolVersion { get; }

        public abstract MqttServerSession CreateSession(INetworkConnection connection, PipeReader reader,
            IObserver<(MqttServerSessionState State, (string topic, byte qosLevel)[] Filters)> subscribeObserver,
            IObserver<(Message Message, string ClientId)> messageObserver);

        public abstract void DispatchMessage(Message message);
    }
}