using static Net.Mqtt.Extensions.SpanExtensions;
using static Net.Mqtt.Extensions.SequenceReaderExtensions;
using Filter = (System.ReadOnlyMemory<byte> Filter, byte Options);

namespace Net.Mqtt.Packets.V5;

public sealed class SubscribePacket : MqttPacketWithId, IMqttPacket5
{
    public SubscribePacket(ushort id, IReadOnlyList<Filter> filters) : base(id)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentOutOfRangeException.ThrowIfZero(filters.Count);

        Filters = filters;
    }

    public IReadOnlyList<Filter> Filters { get; }

    public IReadOnlyList<UserProperty> UserProperties { get; init; }

    public uint? SubscriptionIdentifier { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out ushort id, out uint? subscriptionId,
        out IReadOnlyList<UserProperty> userProperties, out IReadOnlyList<(byte[] Filter, byte Options)> filters)
    {
        var span = sequence.FirstSpan;
        if (length <= span.Length)
        {
            span = span.Slice(0, length);
            id = BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(2);

            if (!TryReadMqttVarByteInteger(span, out var propLen, out var consumed) ||
                !TryReadProperties(span.Slice(consumed, propLen), out subscriptionId, out userProperties))
            {
                goto ret_false;
            }

            span = span.Slice(consumed + propLen);

            var list = new List<(byte[] Filter, byte Options)>();
            while (!span.IsEmpty)
            {
                if (TryReadMqttString(span, out var filter, out var len) && len < span.Length)
                {
                    list.Add((filter, span[len]));
                    span = span.Slice(len + 1);
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
                !TryReadProperties(sequence.Slice(reader.Consumed, propLen), out subscriptionId, out userProperties))
            {
                goto ret_false;
            }

            reader.Advance(propLen);

            var list = new List<(byte[] Filter, byte Options)>();

            while (!reader.End)
            {
                if (TryReadMqttString(ref reader, out var filter) && reader.TryRead(out var qos))
                {
                    list.Add((filter, qos));
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
        subscriptionId = 0;
        userProperties = null;
        return false;
    }

    private static bool TryReadProperties(ReadOnlySpan<byte> span, out uint? subscriptionId, out IReadOnlyList<UserProperty> userProperties)
    {
        userProperties = null;
        subscriptionId = null;
        List<UserProperty> props = null;

        while (!span.IsEmpty)
        {
            switch (span[0])
            {
                case 0x0B:
                    if (subscriptionId is { } || !TryReadMqttVarByteInteger(span.Slice(1), out var i32, out var count))
                        return false;
                    subscriptionId = (uint)i32;
                    span = span.Slice(count + 1);
                    break;
                case 0x26:
                    if (!TryReadMqttString(span.Slice(1), out var key, out count))
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

        userProperties = props;
        return true;
    }

    private static bool TryReadProperties(in ReadOnlySequence<byte> sequence, out uint? subscriptionId, out IReadOnlyList<UserProperty> userProperties)
    {
        userProperties = null;
        subscriptionId = null;
        List<UserProperty> props = null;
        var reader = new SequenceReader<byte>(sequence);

        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x0B:
                    if (subscriptionId is { } || !TryReadMqttVarByteInteger(ref reader, out var i32))
                        return false;
                    subscriptionId = (uint)i32;
                    break;
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key) || !TryReadMqttString(ref reader, out var value))
                        return false;
                    (props ??= []).Add(new(key, value));
                    break;
                default:
                    return false;
            }
        }

        userProperties = props;
        return true;
    }

    #region Implementation of IMqttPacket5

    public int Write([NotNull] IBufferWriter<byte> writer, int maxAllowedBytes = 0)
    {
        var subscriptionId = SubscriptionIdentifier.GetValueOrDefault();
        var propsSize = (subscriptionId is not 0 ? 1 + MqttHelpers.GetVarBytesCount(subscriptionId) : 0) + MqttHelpers.GetUserPropertiesSize(UserProperties);
        var remainingLength = 2 + MqttHelpers.GetVarBytesCount((uint)propsSize) + propsSize;

        var filterCount = Filters.Count;
        for (var i = 0; i < filterCount; i++)
        {
            remainingLength += Filters[i].Filter.Length + 3;
        }

        var size = 1 + MqttHelpers.GetVarBytesCount((uint)remainingLength) + remainingLength;
        var span = writer.GetSpan(size);

        span[0] = PacketFlags.SubscribeMask;
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, remainingLength);
        BinaryPrimitives.WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);

        WriteMqttVarByteInteger(ref span, propsSize);

        if (subscriptionId is not 0)
            WriteMqttVarByteIntegerProperty(ref span, 0x0b, subscriptionId);

        if (UserProperties is { Count: not 0 and var count } properties)
        {
            for (var i = 0; i < count; i++)
            {
                var (name, value) = properties[i];
                WriteMqttUserProperty(ref span, name.Span, value.Span);
            }
        }

        for (var i = 0; i < Filters.Count; i++)
        {
            var (filter, options) = Filters[i];
            WriteMqttString(ref span, filter.Span);
            span[0] = options;
            span = span.Slice(1);
        }

        writer.Advance(size);
        return size;
    }

    #endregion
}