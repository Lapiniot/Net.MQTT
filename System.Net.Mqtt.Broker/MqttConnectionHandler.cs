using System.Buffers;

namespace System.Net.Mqtt.Broker
{
    internal class MqttConnectionHandler : NetworkStreamParser
    {
        public MqttConnectionHandler(INetworkTransport transport) : base(transport)
        {
        }

        protected override void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;
        }

        protected override void OnEndOfStream()
        {
        }

        protected override void OnConnectionAborted()
        {
        }
    }
}