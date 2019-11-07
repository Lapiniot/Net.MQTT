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
            SetHandler(0x02, OnConAck);
            SetHandler(0x03, OnPublish);
            SetHandler(0x04, OnPubAck);
            SetHandler(0x05, OnPubRec);
            SetHandler(0x06, OnPubRel);
            SetHandler(0x07, OnPubComp);
            SetHandler(0x09, OnSubAck);
            SetHandler(0x0B, OnUnsubAck);
            SetHandler(0x0D, OnPingResp);
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