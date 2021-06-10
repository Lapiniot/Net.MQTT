using System.Buffers;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerProtocol : MqttProtocol
    {
        protected internal MqttServerProtocol(NetworkTransport transport) :
            base(transport)
        {
            this[Connect] = OnConnect;
            this[Publish] = OnPublish;
            this[PubAck] = OnPubAck;
            this[PubRec] = OnPubRec;
            this[PubRel] = OnPubRel;
            this[PubComp] = OnPubComp;
            this[Subscribe] = OnSubscribe;
            this[Unsubscribe] = OnUnsubscribe;
            this[PingReq] = OnPingReq;
            this[Disconnect] = OnDisconnect;
        }

        protected abstract void OnConnect(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnPublish(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnPubAck(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnPubRec(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnPubRel(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnPubComp(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnSubscribe(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnUnsubscribe(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnPingReq(byte header, ReadOnlySequence<byte> reminder);

        protected abstract void OnDisconnect(byte header, ReadOnlySequence<byte> reminder);
    }
}