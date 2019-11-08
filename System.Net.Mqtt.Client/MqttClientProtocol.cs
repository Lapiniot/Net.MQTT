using System.Buffers;
using System.IO.Pipelines;
using System.Net.Connections;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Client
{
    public abstract class MqttClientProtocol<TReader> : MqttProtocol<TReader> where TReader : PipeReader
    {
        protected internal MqttClientProtocol(INetworkConnection connection, TReader reader) :
            base(connection, reader)
        {
            SetHandler(ConnAck, OnConnAck);
            SetHandler(Publish, OnPublish);
            SetHandler(PubAck, OnPubAck);
            SetHandler(PubRec, OnPubRec);
            SetHandler(PubRel, OnPubRel);
            SetHandler(PubComp, OnPubComp);
            SetHandler(SubAck, OnSubAck);
            SetHandler(UnsubAck, OnUnsubAck);
            SetHandler(PingResp, OnPingResp);
        }

        protected abstract void OnConnAck(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPublish(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubAck(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubRec(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubRel(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPubComp(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnSubAck(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnUnsubAck(byte header, ReadOnlySequence<byte> remainder);

        protected abstract void OnPingResp(byte header, ReadOnlySequence<byte> remainder);
    }
}