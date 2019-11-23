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