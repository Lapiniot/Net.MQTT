using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Net.Pipelines;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public abstract class MqttBinaryStreamConsumer : PipeConsumer
    {
        private readonly MqttPacketHandler[] handlers;

        protected MqttBinaryStreamConsumer(PipeReader reader) : base(reader)
        {
            handlers = new MqttPacketHandler[16];
        }

        protected MqttPacketHandler this[PacketType index]
        {
            get => handlers[(int)index];
            set => handlers[(int)index] = value;
        }

        protected override bool Consume(in ReadOnlySequence<byte> buffer, out long consumed)
        {
            consumed = 0;

            if(buffer.TryReadMqttHeader(out var flags, out var length, out var offset))
            {
                if(offset + length > buffer.Length) return false;

                var handler = handlers[flags >> 4];

                if(handler == null) throw new InvalidDataException(UnexpectedPacketType);

                handler.Invoke(flags, buffer.Slice(offset, length));

                OnPacketReceived();

                consumed = offset + length;
                return true;
            }

            if(buffer.Length >= 5) throw new InvalidDataException(InvalidDataStream);

            return false;
        }

        protected abstract void OnPacketReceived();
    }
}