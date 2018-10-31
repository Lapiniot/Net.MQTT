using System.Buffers;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;

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

            Handlers[0x01] = OnConnect;
            Handlers[0x02] = OnConAck;
            Handlers[0x03] = OnPublish;
            Handlers[0x04] = OnPubAck;
            Handlers[0x05] = OnPubRec;
            Handlers[0x06] = OnPubRel;
            Handlers[0x07] = OnPubComp;
            Handlers[0x08] = OnSubscribe;
            Handlers[0x09] = OnSubAck;
            Handlers[0x0A] = OnUnsubscribe;
            Handlers[0x0B] = OnUnsubAck;
            Handlers[0x0C] = OnPingReq;
            Handlers[0x0D] = OnPingResp;
            Handlers[0x0E] = OnDisconnect;
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

        public ValueTask<int> SendPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(packet.GetBytes(), cancellationToken);
        }

        public ValueTask<int> SendPacketAsync(byte[] packet, CancellationToken cancellationToken)
        {
            return Transport.SendAsync(packet, cancellationToken);
        }
    }
}