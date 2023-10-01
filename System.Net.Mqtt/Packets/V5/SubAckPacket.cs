using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets.V5;

public sealed class SubAckPacket : MqttPacketWithId, IMqttPacket5
{
    public SubAckPacket(ushort id, ReadOnlyMemory<byte> feedback) : base(id)
    {
        Verify.ThrowIfEmpty(feedback);
        Feedback = feedback;
    }

    public ReadOnlyMemory<byte> Feedback { get; }

    public ReadOnlyMemory<byte> ReasonString { get; init; }

    public IReadOnlyList<Utf8StringPair> UserProperties { get; init; }

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
            List<Utf8StringPair> list = null;
            var props = span.Slice(consumed, propLen);
            while (!props.IsEmpty)
            {
                switch (props[0])
                {
                    case 0x1F:
                        if (reasonString is { } || !TryReadMqttString(props.Slice(1), out var value, out var count))
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
                        (list ??= []).Add(new(key, value));
                        break;
                    default:
                        return false;
                }
            }

            span = span.Slice(consumed + propLen);
            var feedback = span.ToArray();
            packet = new(id, feedback) { ReasonString = reasonString ?? null, UserProperties = list?.AsReadOnly() };
            return true;
        }
        else if (length <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence);

            if (!reader.TryReadBigEndian(out short id) || !TryReadMqttVarByteInteger(ref reader, out var propLen))
                return false;

            ReadOnlyMemory<byte>? reasonString = null;
            List<Utf8StringPair> list = null;
            var props = new SequenceReader<byte>(sequence.Slice(reader.Consumed, propLen));
            while (props.TryRead(out var pid))
            {
                switch (pid)
                {
                    case 0x1F:
                        if (reasonString is { } || !TryReadMqttString(ref props, out var value))
                            return false;
                        reasonString = value;
                        break;
                    case 0x26:
                        if (!TryReadMqttString(ref props, out var key) || !TryReadMqttString(ref props, out value))
                            return false;

                        (list ??= []).Add(new(key, value));
                        break;
                    default:
                        return false;
                }
            }

            reader.Advance(propLen);

            var buffer = new byte[length - reader.Consumed];

            if (!reader.TryCopyTo(buffer))
                return false;

            packet = new((ushort)id, buffer) { ReasonString = reasonString ?? null, UserProperties = list?.AsReadOnly() };

            return true;
        }

        return false;
    }

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer, int maxAllowedBytes)
    {
        var reasonStringSize = ReasonString.Length is not 0 and var rsLen ? 3 + rsLen : 0;
        var userPropertiesSize = GetUserPropertiesSize(UserProperties);
        var propSize = reasonStringSize + userPropertiesSize;

        var size = ComputeAdjustedSizes(maxAllowedBytes, Feedback.Length + 2, ref propSize, ref reasonStringSize, ref userPropertiesSize, out var remainingLength);

        if (size > maxAllowedBytes)
            return 0;

        var span = writer.GetSpan(size);

        span[0] = SubAckMask;
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, remainingLength);
        WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);
        WriteMqttVarByteInteger(ref span, propSize);

        if (reasonStringSize is not 0)
        {
            span[0] = 0x1F;
            span = span.Slice(1);
            WriteMqttString(ref span, ReasonString.Span);
        }

        if (userPropertiesSize is not 0)
        {
            var count = UserProperties.Count;
            for (var i = 0; i < count; i++)
            {
                var (key, value) = UserProperties[i];
                WriteMqttUserProperty(ref span, key.Span, value.Span);
            }
        }

        Feedback.Span.CopyTo(span);

        writer.Advance(size);
        return size;
    }

    #endregion
}