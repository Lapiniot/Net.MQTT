using System.Buffers;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt
{
    public abstract class MqttBinaryStreamProcessor : NetworkPipeProcessor
    {
        protected internal static readonly ValueTask<int> ZeroResult = new ValueTask<int>(0);
        protected readonly MqttPacketHandler[] Handlers;
        protected MqttPacketHandler UnsupportedTypeHandler;

        protected MqttBinaryStreamProcessor(NetworkPipeReader reader) : base(reader)
        {
            Handlers = new MqttPacketHandler[16];
            UnsupportedTypeHandler = SkipUnknownPacket;
        }

        protected override ValueTask<int> ProcessAsync(in ReadOnlySequence<byte> buffer, in CancellationToken cancellationToken)
        {
            if(TryReadByte(buffer, out var flags))
            {
                var handler = Handlers[flags >> 4] ?? UnsupportedTypeHandler;
                return handler(buffer, cancellationToken);
                // TODO: implement threshold check to stop parser at some point if potentially corrupted data detected
            }

            return ZeroResult;
        }

        private ValueTask<int> SkipUnknownPacket(ReadOnlySequence<byte> buffer, CancellationToken token)
        {
            if(TryParseHeader(buffer, out _, out var length, out var offset))
            {
                var size = offset + length;
                if(size <= buffer.Length) return new ValueTask<int>(size);
            }

            return ZeroResult;
        }
    }
}