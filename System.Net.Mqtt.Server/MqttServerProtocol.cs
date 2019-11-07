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
            Handlers[0x01] = OnConnect;
            Handlers[0x03] = OnPublish;
            Handlers[0x04] = OnPubAck;
            Handlers[0x05] = OnPubRec;
            Handlers[0x06] = OnPubRel;
            Handlers[0x07] = OnPubComp;
            Handlers[0x08] = OnSubscribe;
            Handlers[0x0A] = OnUnsubscribe;
            Handlers[0x0C] = OnPingReq;
            Handlers[0x0E] = OnDisconnect;
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