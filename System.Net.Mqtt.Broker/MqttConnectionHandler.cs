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
            throw new NotImplementedException();
        }

        protected override void OnEndOfStream()
        {
            throw new NotImplementedException();
        }

        protected override void OnConnectionAborted()
        {
            throw new NotImplementedException();
        }
    }
}