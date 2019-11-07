using System.Buffers;
using System.IO.Pipelines;
using System.Net.Connections;

namespace System.Net.Mqtt.Client
{
    public abstract class MqttClientProtocol<TReader> : MqttProtocol<TReader> where TReader : PipeReader
    {
        protected internal MqttClientProtocol(INetworkConnection connection, TReader reader) :
            base(connection, reader)
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

        protected abstract void OnConAck(byte header, ReadOnlySequence<byte> remainder);

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