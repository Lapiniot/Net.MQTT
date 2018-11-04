using System.Buffers;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        protected override bool OnSubscribe(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }

        protected override bool OnUnsubscribe(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            throw new NotImplementedException();
        }
    }
}