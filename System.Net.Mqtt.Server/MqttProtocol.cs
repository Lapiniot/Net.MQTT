using System.Buffers;
using System.Net.Pipes;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttProtocol : MqttBinaryStreamHandler
    {
        protected NetworkPipeReader Reader;
        protected INetworkTransport Transport;

        protected internal MqttProtocol(INetworkTransport transport, NetworkPipeReader reader) : base(reader)
        {
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Transport = transport;

            Handlers[0b0001] = OnConnect;
            Handlers[0b0010] = OnConAck;
            Handlers[0b0011] = OnPublish;
            Handlers[0b0100] = OnPubAck;
            Handlers[0b0101] = OnPubRec;
            Handlers[0b0110] = OnPubRel;
            Handlers[0b0111] = OnPubComp;
            Handlers[0b1000] = OnSubscribe;
            Handlers[0b1001] = OnSubAck;
            Handlers[0b1010] = OnUnsubscribe;
            Handlers[0b1011] = OnUnsubAck;
            Handlers[0b1100] = OnPingReq;
            Handlers[0b1101] = OnPingResp;
            Handlers[0b1110] = OnDisconnect;
        }

        protected abstract bool OnDisconnect(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPingResp(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPingReq(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnUnsubAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnUnsubscribe(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnSubAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnSubscribe(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubComp(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubRel(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubRec(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPubAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnPublish(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnConAck(in ReadOnlySequence<byte> buffer, out int consumed);

        protected abstract bool OnConnect(in ReadOnlySequence<byte> buffer, out int consumed);
    }
}