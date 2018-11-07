using System.Buffers;
using System.IO;
using System.Net.Pipes;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public abstract class MqttBinaryStreamProcessor : NetworkPipeProcessor
    {
        protected readonly MqttPacketHandler[] Handlers;

        protected MqttBinaryStreamProcessor(NetworkPipeReader reader) : base(reader)
        {
            Handlers = new MqttPacketHandler[16];
        }

        protected override async ValueTask<int> ProcessAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(TryParseHeader(buffer, out var flags, out var length, out var offset))
            {
                if(offset + length > buffer.Length) return 0;

                var handler = Handlers[flags >> 4];

                if(handler == null) throw new InvalidDataException(UnexpectedPacketType);

                await handler.Invoke(flags, buffer.Slice(offset, length), cancellationToken).ConfigureAwait(false);

                return offset + length;
            }

            if(buffer.Length >= 5) throw new InvalidDataException(InvalidDataStream);

            return 0;
        }
    }
}