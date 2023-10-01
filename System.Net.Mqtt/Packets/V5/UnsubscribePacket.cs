using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets.V5;

public sealed class UnsubscribePacket : MqttPacketWithId, IMqttPacket5
{
    private readonly IReadOnlyList<ReadOnlyMemory<byte>> filters;

    public UnsubscribePacket(ushort id, IReadOnlyList<ReadOnlyMemory<byte>> filters) : base(id)
    {
        Verify.ThrowIfNullOrEmpty(filters);
        this.filters = filters;
    }

    public IReadOnlyList<ReadOnlyMemory<byte>> Filters => filters;

    public IReadOnlyList<Utf8StringPair> UserProperties { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id,
        out IReadOnlyList<Utf8StringPair> userProperties,
        out IReadOnlyList<byte[]> filters)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            span = span.Slice(0, length);
            id = BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(2);

            if (!TryReadMqttVarByteInteger(span, out var propLen, out var consumed) ||
                !TryReadProperties(span.Slice(consumed, propLen), out userProperties))
            {
                goto ret_false;
            }

            span = span.Slice(consumed + propLen);

            var list = new List<byte[]>();
            while (span.Length > 0)
            {
                if (TryReadMqttString(span, out var filter, out consumed))
                {
                    list.Add(filter);
                    span = span.Slice(consumed);
                }
                else
                {
                    goto ret_false;
                }
            }

            filters = list;
            return true;
        }
        else if (length <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence.Slice(0, length));

            if (!reader.TryReadBigEndian(out short local))
                goto ret_false;

            if (!TryReadMqttVarByteInteger(ref reader, out var propLen) ||
                !TryReadProperties(sequence.Slice(reader.Consumed, propLen), out userProperties))
            {
                goto ret_false;
            }

            reader.Advance(propLen);

            var list = new List<byte[]>();

            while (!reader.End)
            {
                if (TryReadMqttString(ref reader, out var filter))
                {
                    list.Add(filter);
                }
                else
                {
                    goto ret_false;
                }
            }

            id = (ushort)local;
            filters = list;
            return true;
        }

    ret_false:
        id = 0;
        filters = null;
        userProperties = null;
        return false;
    }

    private static bool TryReadProperties(ReadOnlySpan<byte> span, out IReadOnlyList<Utf8StringPair> userProperties)
    {
        userProperties = null;
        List<Utf8StringPair> props = null;

        while (!span.IsEmpty)
        {
            switch (span[0])
            {
                case 0x26:
                    if (!TryReadMqttString(span.Slice(1), out var key, out var count))
                        return false;
                    span = span.Slice(count + 1);
                    if (!TryReadMqttString(span, out var value, out count))
                        return false;
                    span = span.Slice(count);
                    (props ??= []).Add(new(key, value));
                    break;
                default:
                    return false;
            }
        }

        userProperties = props?.AsReadOnly();
        return true;
    }

    private static bool TryReadProperties(in ReadOnlySequence<byte> sequence, out IReadOnlyList<Utf8StringPair> userProperties)
    {
        userProperties = null;
        List<Utf8StringPair> props = null;
        var reader = new SequenceReader<byte>(sequence);

        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key) || !TryReadMqttString(ref reader, out var value))
                        return false;
                    (props ??= []).Add(new(key, value));
                    break;
                default:
                    return false;
            }
        }

        userProperties = props?.AsReadOnly();
        return true;
    }

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer, int maxAllowedBytes)
    {
        var propsSize = MqttHelpers.GetUserPropertiesSize(UserProperties);
        var remainingLength = 2 + MqttHelpers.GetVarBytesCount((uint)propsSize) + propsSize;

        var filterCount = filters.Count;
        for (var i = 0; i < filterCount; i++)
        {
            remainingLength += filters[i].Length + 2;
        }

        var size = 1 + MqttHelpers.GetVarBytesCount((uint)remainingLength) + remainingLength;
        var span = writer.GetSpan(size);

        span[0] = UnsubscribeMask;
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, remainingLength);
        BinaryPrimitives.WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);

        WriteMqttVarByteInteger(ref span, propsSize);

        if (UserProperties is { Count: var propCount and > 0 })
        {
            for (var i = 0; i < propCount; i++)
            {
                var (key, value) = UserProperties[i];
                WriteMqttUserProperty(ref span, key.Span, value.Span);
            }
        }

        for (var i = 0; i < filterCount; i++)
        {
            WriteMqttString(ref span, filters[i].Span);
        }

        writer.Advance(size);
        return size;
    }

    #endregion
}