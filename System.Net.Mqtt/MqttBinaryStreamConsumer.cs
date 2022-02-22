using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt;

public abstract class MqttBinaryStreamConsumer : PipeConsumer
{
    private readonly MqttPacketHandler[] handlers;

    protected MqttBinaryStreamConsumer(PipeReader reader) : base(reader) =>
        handlers = new MqttPacketHandler[16];

    protected MqttPacketHandler this[PacketType index]
    {
        get => handlers[(int)index];
        set => handlers[(int)index] = value;
    }

    protected abstract void OnPacketReceived(byte packetType, int totalLength);

    protected sealed override void Consume(in ReadOnlySequence<byte> sequence, out long consumed)
    {
        consumed = 0;

        if (SequenceExtensions.TryReadMqttHeader(in sequence, out var flags, out var length, out var offset))
        {
            var total = offset + length;
            if (total > sequence.Length) return;
            var type = (byte)(flags >> 4);
            var handler = handlers[type];
            if (handler is not null)
                handler.Invoke(flags, sequence.Slice(offset, length));
            else
                ThrowUnexpectedPacketType();
            OnPacketReceived(type, total);
            consumed = total;
        }
        else if (sequence.Length >= 5)
        {
            ThrowInvalidData();
        }
    }

    protected static void ThrowInvalidData() => throw new InvalidDataException(InvalidDataStream);

    protected static void ThrowInvalidPacketFormat(string typeName) => throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, InvalidPacketFormat, typeName));

    protected static void ThrowUnexpectedPacketType() => throw new InvalidDataException(UnexpectedPacketType);
}