using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Net.Pipes;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public abstract class MqttBinaryStreamConsumer : PipeConsumer
    {
        protected readonly MqttPacketHandler[] Handlers;

        protected MqttBinaryStreamConsumer(PipeReader reader) : base(reader)
        {
            Handlers = new MqttPacketHandler[16];
        }

        protected override long Consume(in ReadOnlySequence<byte> buffer)
        {
            if(buffer.TryReadMqttHeader(out var flags, out var length, out var offset))
            {
                if(offset + length > buffer.Length) return 0;

                var handler = Handlers[flags >> 4];

                if(handler == null) throw new InvalidDataException(UnexpectedPacketType);

                handler.Invoke(flags, buffer.Slice(offset, length));

                OnPacketReceived();

                return offset + length;
            }

            if(buffer.Length >= 5) throw new InvalidDataException(InvalidDataStream);

            return 0;
        }

        protected abstract void OnPacketReceived();
    }
}