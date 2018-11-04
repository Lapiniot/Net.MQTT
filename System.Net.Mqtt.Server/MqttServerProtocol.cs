using System.Buffers;
using System.Net.Pipes;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerProtocol : MqttProtocol
    {
        protected internal MqttServerProtocol(INetworkTransport transport, NetworkPipeReader reader) :
            base(transport, reader)
        {
            Handlers[0x01] = OnConnect;
            Handlers[0x03] = OnPublish;
            Handlers[0x04] = OnPubAck;
            Handlers[0x05] = OnPubRec;
            Handlers[0x06] = OnPubRel;
            Handlers[0x07] = OnPubComp;
            Handlers[0x08] = OnSubscribe;
            Handlers[0x0A] = OnUnsubscribe;
            Handlers[0x0C] = OnPingReq;
            Handlers[0x0E] = OnDisconnect;
        }

        protected abstract bool OnConnect(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPublish(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubRec(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubRel(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubComp(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnSubscribe(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnUnsubscribe(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPingReq(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnDisconnect(in ReadOnlySequence<byte> buffer, out int consumed);
    }
}