using System.Buffers;
using System.Net.Pipes;

namespace System.Net.Mqtt.Client
{
    public abstract class MqttClientProtocol : MqttProtocol
    {
        protected internal MqttClientProtocol(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
            Handlers[0x02] = OnConAck;
            Handlers[0x03] = OnPublish;
            Handlers[0x04] = OnPubAck;
            Handlers[0x05] = OnPubRec;
            Handlers[0x06] = OnPubRel;
            Handlers[0x07] = OnPubComp;
            Handlers[0x09] = OnSubAck;
            Handlers[0x0B] = OnUnsubAck;
            Handlers[0x0D] = OnPingResp;
        }

        protected abstract bool OnConAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPublish(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubRec(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubRel(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubComp(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnSubAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnUnsubAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPingResp(in ReadOnlySequence<byte> buffer, out int consumed);
    }
}