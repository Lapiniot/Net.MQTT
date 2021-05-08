using System.Buffers;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Client
{
    public abstract class MqttClientProtocol : MqttProtocol
    {
        protected internal MqttClientProtocol(NetworkTransport transport) : base(transport)
        {
            this[ConnAck] = OnConnAck;
            this[Publish] = OnPublish;
            this[PubAck] = OnPubAck;
            this[PubRec] = OnPubRec;
            this[PubRel] = OnPubRel;
            this[PubComp] = OnPubComp;
            this[SubAck] = OnSubAck;
            this[UnsubAck] = OnUnsubAck;
            this[PingResp] = OnPingResp;
        }

        protected abstract void OnConnAck(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnPublish(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnPubAck(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnPubRec(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnPubRel(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnPubComp(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnSubAck(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnUnsubAck(byte header, ReadOnlySequence<byte> sequence);

        protected abstract void OnPingResp(byte header, ReadOnlySequence<byte> sequence);
    }
}