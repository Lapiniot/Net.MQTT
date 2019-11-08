using System.Buffers;
using System.IO.Pipelines;
using System.Net.Connections;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerProtocol : MqttProtocol<PipeReader>
    {
        protected internal MqttServerProtocol(INetworkConnection connection, PipeReader reader) :
            base(connection, reader)
        {
            SetHandler(Connect, OnConnect);
            SetHandler(Publish, OnPublish);
            SetHandler(PubAck, OnPubAck);
            SetHandler(PubRec, OnPubRec);
            SetHandler(PubRel, OnPubRel);
            SetHandler(PubComp, OnPubComp);
            SetHandler(Subscribe, OnSubscribe);
            SetHandler(Unsubscribe, OnUnsubscribe);
            SetHandler(PingReq, OnPingReq);
            SetHandler(Disconnect, OnDisconnect);
        }

        protected abstract void OnConnect(byte header, ReadOnlySequence<byte> readOnlySequence);

        protected abstract void OnPublish(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubAck(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubRec(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubRel(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubComp(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnSubscribe(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnUnsubscribe(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPingReq(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnDisconnect(byte header, ReadOnlySequence<byte> readOnlySequence);
    }
}