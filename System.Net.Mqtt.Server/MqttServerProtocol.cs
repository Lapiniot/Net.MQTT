using System.Buffers;
using System.IO.Pipelines;
using System.Net.Connections;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerProtocol : MqttProtocol<PipeReader>
    {
        protected internal MqttServerProtocol(INetworkConnection connection, PipeReader reader) :
            base(connection, reader)
        {
            SetHandler(0x01, OnConnect);
            SetHandler(0x03, OnPublish);
            SetHandler(0x04, OnPubAck);
            SetHandler(0x05, OnPubRec);
            SetHandler(0x06, OnPubRel);
            SetHandler(0x07, OnPubComp);
            SetHandler(0x08, OnSubscribe);
            SetHandler(0x0A, OnUnsubscribe);
            SetHandler(0x0C, OnPingReq);
            SetHandler(0x0E, OnDisconnect);
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