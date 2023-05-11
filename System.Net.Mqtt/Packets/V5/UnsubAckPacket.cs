using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Net.Mqtt.Extensions.MqttExtensions;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets.V5;

public sealed class UnsubAckPacket : MqttPacketWithId
{
    public UnsubAckPacket(ushort id, byte[] feedback) : base(id)
    {
        Verify.ThrowIfNullOrEmpty((Array)feedback);

        Feedback = feedback;
    }

    public ReadOnlyMemory<byte> Feedback { get; }

    public ReadOnlyMemory<byte> ReasonString { get; init; }

    public IReadOnlyList<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> Properties { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out SubAckPacket packet)
    {
        packet = null;

        var span = sequence.FirstSpan;

        if (length <= span.Length)
        {
            span = span.Slice(0, length);
            var id = ReadUInt16BigEndian(span);
            span = span.Slice(2);
            if (!TryReadMqttVarByteInteger(span, out var propLen, out var consumed))
                return false;

            ReadOnlyMemory<byte>? reasonString = null;
            List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> list = null;
            var props = span.Slice(consumed, propLen);
            while (!props.IsEmpty)
            {
                switch (props[0])
                {
                    case 0x1F:
                        if (reasonString.HasValue || !TryReadMqttString(props.Slice(1), out var value, out var count))
                            return false;
                        props = props.Slice(count + 1);
                        reasonString = value;
                        break;
                    case 0x26:
                        if (!TryReadMqttString(props.Slice(1), out var key, out count))
                            return false;
                        props = props.Slice(count + 1);
                        if (!TryReadMqttString(props, out value, out count))
                            return false;
                        props = props.Slice(count);
                        (list ??= new()).Add(new(key, value));
                        break;
                    default:
                        return false;
                }
            }

            span = span.Slice(consumed + propLen);
            var feedback = span.ToArray();
            packet = new(id, feedback) { ReasonString = reasonString ?? null, Properties = list?.AsReadOnly() };
            return true;
        }
        else if (length <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence);

            if (!reader.TryReadBigEndian(out short id) || !TryReadMqttVarByteInteger(ref reader, out var propLen))
                return false;

            ReadOnlyMemory<byte>? reasonString = null;
            List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> list = null;
            var props = new SequenceReader<byte>(sequence.Slice(reader.Consumed, propLen));
            while (props.TryRead(out var pid))
            {
                switch (pid)
                {
                    case 0x1F:
                        if (reasonString.HasValue || !TryReadMqttString(ref props, out var value))
                            return false;
                        reasonString = value;
                        break;
                    case 0x26:
                        if (!TryReadMqttString(ref props, out var key) || !TryReadMqttString(ref props, out value))
                            return false;

                        (list ??= new()).Add(new(key, value));
                        break;
                    default:
                        return false;
                }
            }

            reader.Advance(propLen);

            var buffer = new byte[length - reader.Consumed];

            if (!reader.TryCopyTo(buffer))
                return false;

            packet = new((ushort)id, buffer) { ReasonString = reasonString ?? null, Properties = list?.AsReadOnly() };

            return true;
        }

        return false;
    }

    #region Overrides of MqttPacketWithId

    private int GetPropertiesSize() => (!ReasonString.IsEmpty ? ReasonString.Length + 3 : 0) + GetUserPropertiesSize(Properties);

    public override int Write(IBufferWriter<byte> writer, out Span<byte> buffer)
    {
        var propSize = GetPropertiesSize();
        var remainingLength = propSize + GetVarBytesCount((uint)propSize) + Feedback.Length + 2;
        var size = 1 + GetVarBytesCount((uint)remainingLength) + remainingLength;
        var span = buffer = writer.GetSpan(size);

        span[0] = UnsubAckMask;
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, remainingLength);
        WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);
        WriteMqttVarByteInteger(ref span, propSize);

        if (!ReasonString.IsEmpty)
        {
            span[0] = 0x1F;
            span = span.Slice(1);
            WriteMqttString(ref span, ReasonString.Span);
        }

        if (Properties is { Count: > 0 })
        {
            foreach (var (key, value) in Properties)
            {
                WriteMqttUserProperty(ref span, key.Span, value.Span);
            }
        }

        Feedback.Span.CopyTo(span);

        writer.Advance(size);
        return size;
    }

    #endregion
}