using System.Buffers;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttProtocolV3
    {
        protected override bool OnPublish(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubAck(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubRec(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubRel(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnPubComp(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }
    }
}